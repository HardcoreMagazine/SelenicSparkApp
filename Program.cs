using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SelenicSparkApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithRedirects("/Home/Error?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    // Generate roles if not present
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = new string[] { "Admin", "Moderator", "User" };
    for (int i = 0; i < roles.Length; i++)
    {
        if (!await roleManager.RoleExistsAsync(roles[i]))
        {
            await roleManager.CreateAsync(new IdentityRole(roles[i]));
        }
    }

    // Assign roles to base users (such as "admin@selenicspark.org")
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    // Would highly recommend storing logins in separate file and accesing them programmatically
    // Remember to follow password guidelines when creating password
    // [default]: A-Z + a-z + numbers + special characters
    Tuple<string, string, string>[] baseUsers = new Tuple<string, string, string>[]
    {
        new Tuple<string, string, string>("admin@selenicspark.org", "123456aA_", roles[0]),
    };
    
    for (int j = 0; j < baseUsers.Length; j++)
    {
        if (await userManager.FindByEmailAsync(baseUsers[j].Item1) == null)
        {
            var user = new IdentityUser()
            {
                UserName = baseUsers[j].Item1,
                Email = baseUsers[j].Item1,
                EmailConfirmed = true // ----------- DANGER ZONE ----------- //
            };

            await userManager.CreateAsync(user, baseUsers[j].Item2);

            await userManager.AddToRoleAsync(user, baseUsers[j].Item3);
        }
    }
}

app.Run();
