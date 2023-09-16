using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;

namespace SelenicSparkApp.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
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
            if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Author))
            {
                return BadRequest();
            }
            if (post.Title.Length < 4 || post.Title.Length > 300)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                post.CreatedDate = DateTimeOffset.UtcNow;

                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: Posts/Edit
        [Authorize(Roles = "Admin, Moderator")]
        public async Task<IActionResult> Edit(int? id)
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
            return View(post);
        }

        // POST: Posts/Edit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin, Moderator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,PostTitle,PostText,PostAuthor")] Post post)
        {
            if (id != post.PostId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.PostId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: Posts/Delete
        [Authorize(Roles = "Admin, Moderator")]
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Posts/Delete
        [Authorize(Roles = "Admin, Moderator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Post == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Post'  is null.");
            }
            var post = await _context.Post.FindAsync(id);
            if (post != null)
            {
                _context.Post.Remove(post);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
          return (_context.Post?.Any(e => e.PostId == id)).GetValueOrDefault();
        }
    }
}
