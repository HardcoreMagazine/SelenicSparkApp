using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;
using SelenicSparkApp.Views.Admin;

namespace SelenicSparkApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, 
            ILogger<AdminController> logger, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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

        // GET: /Admin/EditUser
        public async Task<IActionResult> EditUser(string? id)
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

            var userExtra = await _context.IdentityUserExpander.FirstOrDefaultAsync(u => u.UID == id);
            // Create entry if none found
            if (userExtra == null)
            {
                // No data available - create & insert new default values
                var defaultUserExtra = new Models.IdentityUserExpander
                {
                    UID = id,
                    User = user,
                    UsernameChangeTokens = 1,
                    UserWarningsCount = 0
                };
                await _context.IdentityUserExpander.AddAsync(defaultUserExtra);
                await _context.SaveChangesAsync();

                var viewModel = new EditUserModel
                {
                    Id = id,
                    UserName = user.UserName!, // No UserName nor Email can be null here, we did check at start
                    Email = user.Email!,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    UsernameChangeTokens = 1,
                    UserWarningsCount = 0
                };
                return View(viewModel);
            }
            else
            {
                var viewModel = new EditUserModel
                {
                    Id = id,
                    UserName = user.UserName!, // No UserName nor Email can be null here, we did check at start
                    Email = user.Email!,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    UsernameChangeTokens = userExtra.UsernameChangeTokens,
                    UserWarningsCount = userExtra.UserWarningsCount
                };
                return View(viewModel);
            }
        }

        // POST: /Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, [Bind("Id,UserName,Email,EmailConfirmed,LockoutEnd," +
            "AccessFailedCount,UsernameChangeTokens,UserWarningsCount")] EditUserModel expandedUser)            
        {
            if (id != expandedUser.Id) // Block cross-editing
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var selectedUser = await _userManager.FindByIdAsync(expandedUser.Id);
                var selectedUserExtras = await _context.IdentityUserExpander.FirstOrDefaultAsync(u => u.UID == id);
                if (selectedUserExtras == null) // Failsafe
                {
                    selectedUserExtras = new Models.IdentityUserExpander
                    {
                        UID = selectedUser!.Id,
                        User = selectedUser!,
                        UsernameChangeTokens = 1,
                        UserWarningsCount = 0
                    };
                }

                if (selectedUser != null)
                {
                    if (selectedUser.UserName != expandedUser.UserName & !string.IsNullOrWhiteSpace(expandedUser.UserName))
                    {
                        // User will see new nickname (Username) only on re-login
                        selectedUser.UserName = expandedUser.UserName;
                        // All 'Normalized' fields must be updated accordingly,
                        // they DO NOT follow their 'Not normal' values on update
                        selectedUser.NormalizedUserName = expandedUser.UserName!.ToUpper();
                    }
                    if (selectedUser.Email != expandedUser.Email & !string.IsNullOrWhiteSpace(expandedUser.Email))
                    {
                        selectedUser.Email = expandedUser.Email;
                        selectedUser.NormalizedEmail = expandedUser.Email!.ToUpper();
                    }
                    if (selectedUser.EmailConfirmed != expandedUser.EmailConfirmed)
                    {
                        selectedUser.EmailConfirmed = expandedUser.EmailConfirmed;
                    }
                    if (selectedUser.LockoutEnd != expandedUser.LockoutEnd) // LockoutEnd can be NULL
                    {
                        selectedUser.LockoutEnd = expandedUser.LockoutEnd;
                    }
                    if (selectedUser.AccessFailedCount != expandedUser.AccessFailedCount & expandedUser.AccessFailedCount >= 0)
                    {
                        selectedUser.AccessFailedCount = expandedUser.AccessFailedCount;
                    }
                    if (selectedUserExtras.UsernameChangeTokens != expandedUser.UsernameChangeTokens & expandedUser.UsernameChangeTokens >= 0)
                    {
                        selectedUserExtras.UsernameChangeTokens = expandedUser.UsernameChangeTokens;
                    }
                    if (selectedUserExtras.UserWarningsCount != expandedUser.UserWarningsCount & expandedUser.UserWarningsCount >= 0)
                    {
                        selectedUserExtras.UserWarningsCount = expandedUser.UserWarningsCount;
                    }

                    // Update extra data fields first
                    _context.IdentityUserExpander.Update(selectedUserExtras);
                    await _context.SaveChangesAsync();

                    // Update main fields
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
                        _logger.LogWarning($"Failed to update {expandedUser.Id}, ErrLog: {errorLog}");
                        return BadRequest();
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            return View(expandedUser);
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

        // GET: /Admin/Roles
        public IActionResult Roles()
        {
            return View();
        }
       
        // POST: /Admin/Roles -- create role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string CreateRoleName)
        {
            if (!string.IsNullOrWhiteSpace(CreateRoleName))
            {
                var role = await _roleManager.FindByNameAsync(CreateRoleName);
                if (role == null)
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(CreateRoleName));
                    if (!result.Succeeded)
                    {
                        string errorLog = "";
                        foreach (var err in result.Errors)
                        {
                            errorLog += $"\"{err.Description}\"; ";
                        }
                        _logger.LogWarning($"Failed to create new role \"{CreateRoleName}\", ErrLog: {errorLog}");
                        return BadRequest();
                    }
                    else
                    {
                        _logger.LogInformation($"Created role \"{CreateRoleName}\"");
                    }
                }
            }
            return RedirectToAction(nameof(Roles));
		}
		
        // GET: /Admin/EditRole
        public async Task<IActionResult> EditRole(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_roleManager.Roles.Any())
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            else
            {
                return View(role);
            }
        }

        // POST: /Admin/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(string id, [Bind("Id", "Name")] IdentityRole role)
        {
            if (id != role.Id) // Block cross-editing
            {
                return NotFound();
            }

            // Default roles are not editable
            if (role.Name == "Admin" ||  role.Name == "Moderator" || role.Name == "User")
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var selectedRole = await _roleManager.FindByIdAsync(role.Id);
                if (selectedRole != null)
                {
                    if (selectedRole.Name !=  role.Name & !string.IsNullOrWhiteSpace(role.Name))
                    {
                        selectedRole.Name = role.Name;
                        selectedRole.NormalizedName = role.Name!.ToUpper();
                        var result = await _roleManager.UpdateAsync(selectedRole);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation($"Role \"{selectedRole.Name}\" has been updated");
                            return RedirectToAction(nameof(Roles));
                        }
                        else
                        {
                            string errorLog = "";
                            foreach (var err in result.Errors)
                            {
                                errorLog += $"\"{err.Description}\"; ";
                            }
                            _logger.LogInformation($"Failed to update \"{selectedRole.Name}\", ErrLog: {errorLog}");
                            return BadRequest();
                        }
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            // We got this far? Go back.
            return View(role);
        }

        // GET: /Admin/DeleteRole
        public async Task<IActionResult> DeleteRole(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_context.Roles.Any())
            {
                return NotFound();
            }
            var role = await _context.FindAsync<IdentityRole>(id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }

        // POST: /Admin/DeleteRole
        [HttpPost]
        [ActionName("DeleteRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoleConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_context.Roles.Any())
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            if (role.Name == "Admin" || role.Name == "Moderator" || role.Name == "User")
            {
                return BadRequest();
            }
            _logger.LogInformation($"Deleted role \"{role.Name}\"");
            await _roleManager.DeleteAsync(role);
            return RedirectToAction(nameof(Roles));
        }
    }
}
