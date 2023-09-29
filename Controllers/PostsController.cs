using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using Markdig;
using ReverseMarkdown;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;
using SelenicSparkApp.Views.Posts;

namespace SelenicSparkApp.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly Converter _converter;
        private readonly ILogger<PostsController> _logger;

        private const int MinPostTitleLen = 4;
        private const int MaxPostTitleLen = 300;
        private const int MaxPostTextLen = 20_000;

        private const int MinCommentLen = 3;
        private const int MaxCommentLen = 3000;

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

        // GET: FormattingGuide
        public IActionResult FormattingGuide()
        {
            return View();
        }

        // GET: Posts/Search
        public IActionResult Search() // TODO: multi-page view
        {
            // Initialize input values
            ViewBag.SearchPhrase = "";
            ViewBag.Filter = "Everything";
            return View();
        }

        // POST: Posts/SearchResults
        public async Task<IActionResult> SearchResults(string SearchPhrase, string Filter) // TODO: multi-page view
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

            var comments = await _context.Comment
                .Where(c => c.PostId == post.PostId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            var fullPost = new DetailsModel { Post = post, Comments = comments };
            return View(fullPost);
        }

        // POST: Post/Details -- add comment
        [Authorize]
        [HttpPost, ActionName("Comment")]
        public async Task<IActionResult> PostComment([Bind("PostId, Author, Text")] Comment partialComment)
        {
            // Post indexing starts at 1; '0' is a default INT value when no other value been provided
            if (partialComment.PostId == 0) 
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(partialComment.Text) || string.IsNullOrWhiteSpace(partialComment.Author))
            {
                return RedirectToAction(nameof(Details), new { id = partialComment.PostId });
            }

            if (partialComment.Author.Length < MinCommentLen)
            {
                return BadRequest();
            }

            if (partialComment.Text.Length > MaxCommentLen)
            {
                partialComment.Text = partialComment.Text[0..MaxCommentLen];
            }

            var comment = new Comment
            {
                PostId = partialComment.PostId,
                Author = partialComment.Author,
                Text = partialComment.Text,
                CreatedDate = DateTimeOffset.UtcNow
            };
            await _context.Comment.AddAsync(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = partialComment.PostId });
        }

        // POST: Post/Details -- del comment
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int? cid, int? pid, bool? GiveWarning)
        {
            if (cid == null || cid == 0)
            {
                return BadRequest();
            }
            if (pid == null || pid == 0)
            {
                return BadRequest();
            }

            var comment = await _context.Comment.FindAsync(cid);
            if (comment != null)
            {
                if (comment.Author != User.Identity?.Name & !User.IsInRole("Admin") & !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }

                if (GiveWarning == true)
                {
                    var user = await _userMgr.FindByNameAsync(comment.Author);
                    if (user != null)
                    {
                        await WarnUser(user);
                    }
                }

                _logger.LogInformation($"User '{User.Identity?.Name}' deleted comment: cid={cid} pid={pid}");
                _context.Comment.Remove(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = pid });
        }

        // GET: Posts/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Posts/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Text,Author")] Post post)
        {
            // Double checking so people with Postman and other tools won't make a mess
            if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Author))
            {
                return View(post);
            }
            
            if (post.Title.Length < MinPostTitleLen)
            {
                return View(post);
            }          
            if (post.Title.Length > MaxPostTitleLen)
            {
                post.Title = post.Title[0..MaxPostTitleLen];
            }

            if (post.Text?.Length > MaxPostTextLen)
            {
                post.Text = post.Text[0..MaxPostTextLen];
            }

            if (ModelState.IsValid)
            { 
                post.CreatedDate = DateTimeOffset.UtcNow;
                if (!string.IsNullOrWhiteSpace(post.Text)) // Text will be processed regardless, Post.Text is nullable
                {
                    // Strip all javascript input
                    // Alternative pattens (all tested on 2 simple scripts):
                    // @"(?s)<\s?script.*?(/\s?>|<\s?/\s?script\s?>)"
                    // @"(?s)<script.*?(/>|</script>)"
                    post.Text = Regex.Replace(post.Text, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseEmphasisExtras() // Strikethrough support
                        .UseBootstrap()
                        .UseEmojiAndSmiley(false)
                        .Build();
                    post.Text = Markdown.ToHtml(post.Text, pipeline);
                }
                _context.Post.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User '{User.Identity?.Name}' CREATED post: '{post.Title}'. ");
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
                if (post.Author != User.Identity?.Name && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }
                
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
                    if (partialPost.Text.Length > MaxPostTextLen)
                    {
                        partialPost.Text = partialPost.Text[..MaxPostTextLen]; // .Substring(0, MaxPostLen);
                    }
                    // Strip all javascript input
                    // Alternative pattens (all tested on 2 simple scripts):
                    // @"(?s)<\s?script.*?(/\s?>|<\s?/\s?script\s?>)"
                    // @"(?s)<script.*?(/>|</script>)"
                    partialPost.Text = Regex.Replace(partialPost.Text, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);

                    var pipeline = new MarkdownPipelineBuilder()
                        .UseEmphasisExtras()
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
                        await WarnUser(user);
                    }
                }

                post.Text = partialPost.Text;
                _context.Post.Update(post);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User '{User.Identity?.Name}' EDITED post: '{post.Title}'. ");
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

            if (post.Author != User.Identity?.Name && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            // Warn user if GiveWarning flag is set and equals to 'true'
            if (GiveWarning == true) // GiveWarning is nullable
            {
                var user = await _userMgr.FindByNameAsync(post.Author);
                if (user != null)
                {
                    await WarnUser(user);
                }
            }
            _context.Post.Remove(post);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User '{User.Identity?.Name}' DELETED post: '{post.Title}'. ");
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

        /// <summary>
        /// Attempt to add warning to user's IdentityUserExpander entry
        /// </summary>
        /// <param name="user">Target user</param>
        private async Task WarnUser(IdentityUser user)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return;
            }

            var roles = await _userMgr.GetRolesAsync(user);
            bool allowWarning = 
                roles == null || 
                (roles != null && roles.Contains("Moderator") && User.IsInRole("Admin")) || 
                (roles != null && roles.Contains("User"));

            if (allowWarning)
            {
                var userExtra = await _context.IdentityUserExpander.FindAsync(user.Id);
                if (userExtra == null)
                {
                    _logger.LogWarning($"User '{User.Identity?.Name} tried to WARN user {user.UserName}, " +
                        $"but 'IdentityUserExpander' entry was not found");
                    return;
                }
                userExtra.UserWarningsCount += 1;
                // Ban user for 7d on every fifth warning
                if (userExtra.UserWarningsCount != 0 & userExtra.UserWarningsCount % 5 == 0)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddDays(7);
                    await _userMgr.UpdateAsync(user);
                    _logger.LogInformation($"User '{User.Identity?.Name}' WARNED user: '{user.UserName}', which resulted in 7d ban. ");
                }
                else
                {
                    _logger.LogInformation($"User '{User.Identity?.Name}' WARNED user: '{user.UserName}'. ");
                }
                _context.IdentityUserExpander.Update(userExtra); // Changes saved outside func
            }
        }
    }
}
