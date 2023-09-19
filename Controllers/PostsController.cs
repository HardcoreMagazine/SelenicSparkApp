using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Markdig;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;
using ReverseMarkdown;

namespace SelenicSparkApp.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Converter _converter;
        private const int MaxPostLen = 20_000;

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
        public async Task<IActionResult> Index()
        {
            return _context.Post != null ? View(await _context.Post.ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post' is null.");
        }

        // GET: Posts/Search
        public IActionResult Search()
        {
            // Initialize input values
            ViewBag.SearchPhrase = "";
            ViewBag.Filter = "Titles";
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
            switch (Filter)
            {
                case "Titles":
                    return _context.Post != null ?
                        View("Search", await _context.Post
                        .Where(p => p.Title.Contains(SearchPhrase))
                        .ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post'  is null.");
                case "Text":
                    return _context.Post != null ?
                        View("Search", await _context.Post
                        .Where(p => p.Text.Contains(SearchPhrase))
                        .ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post'  is null.");
                case "Author":
                    return _context.Post != null ?
                        View("Search", await _context.Post
                        .Where(p => p.Author.Contains(SearchPhrase))
                        .ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post'  is null.");
                default:
                    return _context.Post != null ?
                        View("Search", await _context.Post
                        .Where(p => p.Title.Contains(SearchPhrase) || p.Text.Contains(SearchPhrase) || p.Author.Contains(SearchPhrase))
                        .ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post'  is null.");
            }
        }

        // GET: Posts/Details/5
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
    }
}
