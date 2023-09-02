// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SelenicSparkApp.Data;
using SelenicSparkApp.Models;

namespace SelenicSparkApp.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        //public string Username { get; set; } // override by Input.Username

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        public int UsernameChangeTokens { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [DataType(DataType.Text)]
            [StringLength(24, ErrorMessage = "{0} must be {2} to {1} characters long.", MinimumLength = 4)]
            [Display(Name = "Username")]
            public string Username { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            //[Phone]
            //[Display(Name = "Phone number")]
            //public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var username = await _userManager.GetUserNameAsync(user);
            //var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var usernameChangeTokens = await _context.IdentityUserExpander.FirstOrDefaultAsync(u => u.UID == user.Id);
            if (usernameChangeTokens == null)
            {
                usernameChangeTokens = new IdentityUserExpander(user.Id, user);
                await _context.IdentityUserExpander.AddAsync(usernameChangeTokens);
                await _context.SaveChangesAsync();
                // Default amount of tokens is 1
                UsernameChangeTokens = 1;
            }
            else
            {
                UsernameChangeTokens = usernameChangeTokens.UsernameChangeTokens;
            }
            Input = new InputModel
            {
                //PhoneNumber = phoneNumber
                Username = username
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Input.Username) & Input.Username != user.UserName)
            {
                if (Input.Username.Length < 4 || Input.Username.Length > 24)
                {
                    StatusMessage = "Error: Username length must be from 4 to 24 characters long.";
                    return RedirectToPage();
                }
                if (await _userManager.FindByNameAsync(Input.Username) != null)
                {
                    StatusMessage = $"Error: Username \"{Input.Username}\" is alrady taken.";
                    return RedirectToPage();
                }
                if (Input.Username.ToLower().Contains("admin") ||
                    Input.Username.ToLower().Contains("moderator") ||
                    Input.Username.ToLower().Contains("support"))
                {
                    StatusMessage = $"Error: Username \"{Input.Username}\" is alrady taken.";
                }

                var uct = await _context.IdentityUserExpander.FirstAsync(u => u.UID == user.Id);
                if (uct.UsernameChangeTokens <= 0)
                {
                    StatusMessage = "Error: you don't have any username change tokens left!";
                    return RedirectToPage();
                }

                user.UserName = Input.Username;
                user.NormalizedUserName = Input.Username.ToUpper();
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    StatusMessage = "Error: Unexpected error when trying to set username.";
                    return RedirectToPage();
                }
                else
                {
                    uct.UsernameChangeTokens -= 1;
                    _context.IdentityUserExpander.Update(uct);
                    await _context.SaveChangesAsync();
                    StatusMessage = "Your profile has been updated.";
                }
            }
/*
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }
*/
            // This little line allows partial page view (username displayed in top right corner) to reload
            // so user wouldn't have to re-login to see username change
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToPage();
        }
    }
}
