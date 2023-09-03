using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SelenicSparkApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(UserManager<IdentityUser> userManager, ILogger<AdminController> logger)
        {
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
            if (id == null || !_userManager.Users.Any())
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
                        // User will see new nickname (Username) only on re-login
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
                    if (selectedUser.AccessFailedCount != user.AccessFailedCount & user.AccessFailedCount >= 0)
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

        // GET: /Admin/DeleteUser
        public async Task<IActionResult> DeleteUser(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_userManager.Users.Any())
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

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_userManager.Users.Any())
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = await _userManager.GetRolesAsync(user);
            // Normally user would have just 1 role, but just in case...
            foreach (var role in roles)
            {
                if (role == "Admin")
                {
                    return BadRequest();
                }
            }
            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Users));
        }
    }
}
