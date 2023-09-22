using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace SelenicSparkApp.CustomClasses
{
    namespace SelenicSparkApp.CustomClasses
    {
        /// <summary>
        /// Middleware that tracks every request made by authenticated user, 
        /// that would redirect to Lockout page on next request the instant user 
        /// gets suspended
        /// </summary>
        public class UserBanManager
        {
            private readonly RequestDelegate _next;

            public UserBanManager(RequestDelegate next)
            {
                _next = next;
            }

            private static async Task<bool> IsBannedAsync(HttpContext context)
            {
                var userManager = context.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                var user = await userManager.GetUserAsync(context.User);
                return user != null && user.LockoutEnd > DateTime.UtcNow;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                if (context.User.Identity != null)
                {
                    if (context.User.Identity.IsAuthenticated)
                    {
                        if (await IsBannedAsync(context))
                        {
                            // Seamless log-out with redirect
                            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                            //await context.SignOutAsync(IdentityConstants.ExternalScheme); // 3rd-party auth (remove if not needed)

                            var redirectHtml = "<meta http-equiv=\"Refresh\" content=\"0; url='/Identity/Account/Lockout'\" />";
                            context.Response.StatusCode = StatusCodes.Status200OK;
                            context.Response.ContentType = "text/html";
                            await context.Response.WriteAsync(redirectHtml);
                        }
                    }
                }
                await _next(context);
            }
        }
    }
}
