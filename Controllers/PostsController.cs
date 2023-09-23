using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Markdig;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;
using ReverseMarkdown;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using SelenicSparkApp.Views.Posts;

namespace SelenicSparkApp.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly Converter _converter;
        private readonly ILogger<PostsController> _logger;
        private const int MaxPostLen = 20_000;
        private const int PostsPerPage = 25; // Default: 25
        private const int PostPreviewTextLen = 450;
        private const int MaxSearchPhraseLen = 64;

        public PostsController(ApplicationDbContext context, UserManager<IdentityUser> userMgr, ILogger<PostsController> logger)
        {
            _context = context;
            _userMgr = userMgr;
            _logger = logger;

            var config = new Config
            {
                ListBulletChar = '*',
                SmartHrefHandling = true
            };
            _converter = new Converter(config);
        }

        // GET: Posts
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!_context.Post.Any())
            {
                return View(); // Null view
            }
            else
            {
                int pagesTotal = (int)Math.Ceiling((double)_context.Post.Count() / PostsPerPage);

                if (page > pagesTotal)
                {
                    ViewBag.Page = null;
                    return View();
                }

                int skip = (page - 1) * PostsPerPage;
                var posts = await _context.Post
                    .OrderByDescending(p => p.CreatedDate)
                    .Skip(skip)
                    .Take(PostsPerPage)
                    .ToListAsync();

                ProcessText(ref posts);

                ViewBag.Page = page;
                ViewBag.Pages = pagesTotal;

                return View(posts);
            }
        }

        // GET: Posts/Search
        public IActionResult Search()
        {
            // Initialize input values
            ViewBag.SearchPhrase = "";
            ViewBag.Filter = "Everything";
            return View();
        }

        // POST: Posts/SearchResults
        public async Task<IActionResult> SearchResults(string SearchPhrase, string Filter)
        {
            // Save-set last form state (works like magic)
            // ViewBag would still reset if page is reloaded, obviously
            // See './Views/Posts/Search.cshtml' for more
            ViewBag.SearchPhrase = SearchPhrase;
            ViewBag.Filter = Filter;

            // Null, length, db checking
            if (string.IsNullOrWhiteSpace(SearchPhrase))
            {
                return View("Search", new List<Post>());
            }
            else if (SearchPhrase.Length > MaxSearchPhraseLen)
            {
                return View("Search", new List<Post>());
            }
            else if (!_context.Post.Any())
            {
                return View("Search", new List<Post>());
            }

            // Filter out results
            if (Filter == "Titles")
            {
                var posts = await _context.Post
                    .Where(p => p.Title.Contains(SearchPhrase))
                    .ToListAsync();
                ProcessText(ref posts); // 'ProcessText' function returns void!!
                return View("Search", posts);
            }
            else if (Filter == "Author")
            {
                var posts = await _context.Post
                    .Where(p => p.Author.Contains(SearchPhrase))
                    .ToListAsync();
                ProcessText(ref posts);
                return View("Search", posts);
            }
            else if (Filter == "Text")
            {
                var posts = await _context.Post
                    .Where(p => string.IsNullOrWhiteSpace(p.Text) ? false : p.Text.Contains(SearchPhrase))
                    .ToListAsync();
                ProcessText(ref posts);
                return View("Search", posts);
            }
            else
            {
                var posts = await _context.Post
                    .Where(p =>
                        p.Title.Contains(SearchPhrase) ||
                        (string.IsNullOrWhiteSpace(p.Text) ? false : p.Text.Contains(SearchPhrase)) ||
                        p.Author.Contains(SearchPhrase))
                    .ToListAsync();
                ProcessText(ref posts);
                return View("Search", posts);
            }
        }

        // GET: Posts/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Post == null)
            {
                return NotFound();
            }

            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var comments = await _context.Comment.Where(c => c.PostId == post.PostId).ToListAsync();

            var fullPost = new DetailsModel { Post = post, Comments = comments };
            return View(fullPost);
        }

        // POST: Post/Details -- comment
        [Authorize]
        [HttpPost, ActionName("Comment")]
        public async Task<IActionResult> PostComment([Bind("PostId, Author, Text")] Comment partialComment)
        {
            if (string.IsNullOrWhiteSpace(partialComment.Text) || string.IsNullOrWhiteSpace(partialComment.Author))
            {
                return BadRequest();
            }

            var comment = new Comment
            {
                CommentId = "",
                PostId = partialComment.PostId,
                Author = partialComment.Author,
                Text = partialComment.Text,
                CreatedDate = DateTimeOffset.UtcNow
            };
            await _context.Comment.AddAsync(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = partialComment.PostId });
        }

        // GET: Posts/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Text,Author")] Post post)
        {
            // Double checking so people with Postman and other tools won't make a mess
            if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Author))
            {
                return BadRequest();
            }
            if (post.Title.Length < 4 || post.Title.Length > 300)
            {
                return BadRequest();
            }
            if (post.Text?.Length > MaxPostLen)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                post.CreatedDate = DateTimeOffset.UtcNow;
                if (!string.IsNullOrWhiteSpace(post.Text)) // Text will be processed regardless, Post.Text is nullable
                {
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseBootstrap()
                        .UseEmojiAndSmiley(false)
                        .Build();
                    post.Text = Markdown.ToHtml(post.Text, pipeline);
                }
                _context.Post.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User \"{User.Identity?.Name}\" CREATED post: \"{post.Title}\". ");
                return RedirectToAction(nameof(Details), routeValues: new { id = post.PostId });
            }
            return View(post); // Return to self
        }

        // GET: Posts/Edit
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || !_context.Post.Any())
            {
                return NotFound();
            }

            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(User.Identity?.Name)) // Null check
            {
                return Forbid();
            }

            var user = await _userMgr.FindByNameAsync(User.Identity.Name);
            if (user == null) // Another null check
            {
                return Forbid();
            }

            if (post.Author ==  user.UserName || User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                // Reverse-translate HTML to Markdown
                if (!string.IsNullOrWhiteSpace(post.Text))
                {
                    post.Text = _converter.Convert(post.Text);
                }
                return View(post);
            }
            else
            {
                return Forbid();
            }
        }

        // POST: Posts/Edit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, bool? GiveWarning, [Bind("PostId,Title,Text")] Post partialPost)
        {
            if (id != partialPost.PostId)
            {
                return BadRequest();
            }

            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            else
            {
                // Process Title (4 < len < 300, not null)
                if (!string.IsNullOrWhiteSpace(partialPost.Title))
                {
                    if (partialPost.Title != post.Title && (partialPost.Title.Length > 4 && partialPost.Title.Length < 300))
                    {
                        post.Title = partialPost.Title;
                    }
                }
                // Process Text (len < MaxPostLen, nullable)
                if (!string.IsNullOrWhiteSpace(partialPost.Text))
                {
                    // Trim if exeeds maximum length
                    if (partialPost.Text.Length > MaxPostLen)
                    {
                        partialPost.Text = partialPost.Text[..MaxPostLen]; // .Substring(0, MaxPostLen);
                    }
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseBootstrap()
                        .UseEmojiAndSmiley(false)
                        .Build();
                    partialPost.Text = Markdown.ToHtml(partialPost.Text, pipeline);
                }

                // Warn user if GiveWarning flag is set and equals to 'true'
                if (GiveWarning == true) // GiveWarning is nullable
                {
                    var user = await _userMgr.FindByNameAsync(post.Author);
                    if (user != null)
                    {
                        var roles = await _userMgr.GetRolesAsync(user);
                        // Users with 'Admin' and 'Moderator' role will not recieve any warnings
                        if (roles != null && !(roles.Contains("Admin") || roles.Contains("Moderator")))
                        {
                            var userExtra = await _context.IdentityUserExpander.FindAsync(user.Id);
                            // Cannot be null since user logged in at least once and posted something
                            // But since we're using visual studio and I'm annoyed by curvy underline...
                            if (userExtra != null)
                            {
                                userExtra.UserWarningsCount += 1;
                                // Ban user for 7d on every fifth warning
                                if (userExtra.UserWarningsCount != 0 & userExtra.UserWarningsCount % 5 == 0)
                                {
                                    user.LockoutEnd = DateTime.UtcNow.AddDays(7);
                                }
                                await _userMgr.UpdateAsync(user); // We probably don't need that. Probably.
                                _logger.LogInformation($"User \"{User.Identity?.Name}\" WARNED user: \"{post.Author}\". ");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"User \"{User.Identity?.Name}\" tried to WARN user: \"{post.Author}\". ");
                        }
                    }
                }

                post.Text = partialPost.Text;
                _context.Post.Update(post);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User \"{User.Identity?.Name}\" EDITED post: \"{post.Title}\". ");
                return RedirectToAction(nameof(Details), new { id = partialPost.PostId });
            }
        }

        // GET: Posts/Delete
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || !_context.Post.Any())
            {
                return NotFound();
            }

            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            if (post.Author == User.Identity?.Name || User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                return View(post);
            }
            else
            {
                return Forbid();
            }
        }

        // POST: Posts/Delete
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool? GiveWarning)
        {
            if (!_context.Post.Any())
            {
                return NotFound();
            }
            var post = await _context.Post.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            if (post.Author != User.Identity?.Name & !User.IsInRole("Admin") & !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            // Warn user if GiveWarning flag is set and equals to 'true'
            if (GiveWarning == true) // GiveWarning is nullable
            {
                var user = await _userMgr.FindByNameAsync(post.Author);
                if (user != null)
                {
                    var roles = await _userMgr.GetRolesAsync(user);
                    // Users with 'Admin' and 'Moderator' role will not recieve any warnings
                    if (roles != null && !(roles.Contains("Admin") || roles.Contains("Moderator")))
                    {
                        var userExtra = await _context.IdentityUserExpander.FindAsync(user.Id);
                        // Cannot be null since user logged in at least once and posted something
                        // But since we're using visual studio and I'm annoyed by curvy underline...
                        if (userExtra != null) 
                        {
                            userExtra.UserWarningsCount += 1;
                            // Ban user for 7d on every fifth warning
                            if (userExtra.UserWarningsCount != 0 & userExtra.UserWarningsCount % 5 == 0)
                            {
                                user.LockoutEnd = DateTime.UtcNow.AddDays(7);
                            }
                            await _userMgr.UpdateAsync(user); // We probably don't need that. Probably.
                            _logger.LogInformation($"User \"{User.Identity?.Name}\" WARNED user: \"{post.Author}\". ");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"User \"{User.Identity?.Name}\" tried to WARN user: \"{post.Author}\". ");
                    }
                }
            }
            _context.Post.Remove(post);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User \"{User.Identity?.Name}\" DELETED post: \"{post.Title}\". ");
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Processes 'Post.Text' field for provided list of posts, 
        /// removing HTML tags and trimming it by PostPreviewTextLen
        /// </summary>
        /// <param name="posts">Reference (pointer) to posts list</param>
        private void ProcessText(ref List<Post>? posts)
        {
            if (posts != null)
            {
                for (int i = 0; i < posts.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(posts[i].Text))
                    {
                        posts[i].Text = Regex.Replace(posts[i].Text!, "<.*?>", string.Empty); // Strip all HTML tags
                        if (posts[i].Text!.Length > PostPreviewTextLen)
                        {
                            posts[i].Text = posts[i].Text![..PostPreviewTextLen] + "...";
                        }
                    }
                }
            }
        }
    }
}
