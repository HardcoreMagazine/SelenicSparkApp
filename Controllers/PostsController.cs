using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Markdig;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;
using ReverseMarkdown;
using System.Text.RegularExpressions;

namespace SelenicSparkApp.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Converter _converter;
        private const int MaxPostLen = 20_000;
        private const int PostsPerPage = 25; // Default: 25
        private const int PostPreviewTextLen = 450;
        private const int MaxSearchPhraseLen = 64;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;

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

                if(page > pagesTotal)
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

            var post = await _context.Post
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
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
                _context.Add(post);
                await _context.SaveChangesAsync();

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
            
            if (post.Author ==  User.Identity?.Name || User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                // Reverse-translate HTML to Markdown
                post.Text = _converter.Convert(post.Text);
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
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Title,Text")] Post partialPost)
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
                post.Text = partialPost.Text;
                _context.Post.Update(post);
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> DeleteConfirmed(int id)
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
            if (post.Author == User.Identity?.Name || User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                _context.Post.Remove(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return Forbid();
            }
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
