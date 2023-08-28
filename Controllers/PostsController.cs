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
              return _context.Post != null ? 
                          View(await _context.Post.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.Post'  is null.");
        }

        // GET: Posts/Search
        public async Task<IActionResult> Search()
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
            if (Filter == "Titles")
            {
                return _context.Post != null ?
                    View("Search", await _context.Post
                    .Where(p => p.PostTitle.Contains(SearchPhrase))
                    .ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post'  is null.");
            }
            else if (Filter == "Text")
            {
                return _context.Post != null ?
                    View("Search", await _context.Post
                    .Where(p => p.PostText.Contains(SearchPhrase))
                    .ToListAsync()) : Problem("Entity set 'ApplicationDbContext.Post'  is null.");
            }
            else // No filter - everything
            {
                return _context.Post != null ?
                    View("Search", await _context.Post
                    .Where(p => p.PostTitle.Contains(SearchPhrase) || p.PostText.Contains(SearchPhrase))
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
        public async Task<IActionResult> Create([Bind("PostId,PostTitle,PostText")] Post post)
        {
            if (ModelState.IsValid)
            {
                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: Posts/Edit/5
        [Authorize]
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

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,PostTitle,PostText")] Post post)
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

        // GET: Posts/Delete/5
        [Authorize]
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

        // POST: Posts/Delete/5
        [Authorize]
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
