using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelenicSparkApp.Data;

namespace SelenicSparkApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminController> _logger;


        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Admin
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Users
        public ActionResult Users()
        {
            return View();
        }

        // GET: /Admin/User  -- edit user page
        public async Task<IActionResult> User(string? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: /Admin/User -- edit user page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> User(string id, [Bind("Id,UserName,Email,EmailConfirmed,LockoutEnd,AccessFailedCount")] IdentityUser user)
        {
            if (id != user.Id) // Block cross-editing
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var selectedUser = await _userManager.FindByIdAsync(user.Id);
                if (selectedUser != null)
                {
                    if (selectedUser.UserName != user.UserName & !string.IsNullOrWhiteSpace(user.UserName))
                    {
                        selectedUser.UserName = user.UserName;
                        // All 'Normalized' fields must be updated accordingly,
                        // they DO NOT follow their 'Not normal' values on update
                        selectedUser.NormalizedUserName = user.UserName!.ToUpper();
                    }
                    if (selectedUser.Email != user.Email & !string.IsNullOrWhiteSpace(user.Email))
                    {
                        selectedUser.Email = user.Email;
                        selectedUser.NormalizedEmail = user.Email!.ToUpper();
                    }
                    if (selectedUser.EmailConfirmed != user.EmailConfirmed)
                    {
                        selectedUser.EmailConfirmed = user.EmailConfirmed;
                    }
                    if (selectedUser.LockoutEnd != user.LockoutEnd) // LockoutEnd can be NULL
                    {
                        selectedUser.LockoutEnd = user.LockoutEnd;
                    }
                    if (selectedUser.AccessFailedCount != user.AccessFailedCount)
                    {
                        selectedUser.AccessFailedCount = user.AccessFailedCount;
                    }

                    var result = await _userManager.UpdateAsync(selectedUser);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User {selectedUser.Id} has been updated");
                        return RedirectToAction(nameof(Users));
                    }
                    else
                    {
                        string errorLog = "";
                        foreach (var err in result.Errors)
                        {
                            errorLog += $"{err.Description}; ";
                        }
                        _logger.LogWarning($"Failed to update {user.Id}, ErrLog: {errorLog}");
                        return BadRequest();
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            return View(user);
        }

        public void DeleteUser(string? id)
        {
            //TODO
        }
    }
}
