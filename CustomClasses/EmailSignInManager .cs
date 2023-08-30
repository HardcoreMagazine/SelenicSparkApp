using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

namespace SelenicSparkApp.CustomClasses
{
    /// <summary>
    /// Provides custom API for user to log in using EMAIL instead of USERNAME. 
    /// Inherited from SignInManager<IdentityUser>. Please check source code for 
    /// additional notes
    /// </summary>
    public class EmailSignInManager : SignInManager<IdentityUser>
    {
        // Make sure to register SignInManager in 'Program.cs' (startup file):
        // builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
        //    .AddRoles<IdentityRole>()
        //    .AddSignInManager<EmailSignInManager>() // <--- Custom sign-in manager
        //    .AddEntityFrameworkStores<ApplicationDbContext>();
        public EmailSignInManager(
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<IdentityUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<IdentityUser> confirmation
            ) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override async Task<SignInResult> PasswordSignInAsync(string userName, string password,
            bool isPersistent, bool lockoutOnFailure)
        {
            var user = await UserManager.FindByEmailAsync(userName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        }
    }
}
