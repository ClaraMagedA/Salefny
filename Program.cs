using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.Models.Options;
using SalefnyMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Database Context (SQL Server) using the connection string from appsettings.json
builder.Services.AddDbContext<SalefnyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Add ASP.NET Core Identity & Entity Framework Stores
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;
})
    .AddEntityFrameworkStores<SalefnyDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
});

// 3. Register Custom Application Services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IRatingService, RatingService>();

// OTP delivery: SMTP email for login, Egyptian SMS gateway for identity verification.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<SmsGatewayOptions>(builder.Configuration.GetSection("SmsGateway"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<ISmsSender, EgyptianSmsSender>();

// 4. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Session is used to hold the identity-verification OTP while the user confirms it.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed the four account types and the dedicated admin/super-admin accounts.
// Normal registration always starts as Renter; the other roles are granted explicitly.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<SalefnyDbContext>();

    foreach (var role in AppRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    async Task EnsureRoleAsync(ApplicationUser user, string role)
    {
        if (await userManager.IsInRoleAsync(user, role))
            return;

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            app.Logger.LogError("Role assignment failed for {Email}/{Role}: {Errors}",
                user.Email, role, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    async Task<ApplicationUser?> EnsureAccountAsync(
        string email, string password, string fullName, string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                City = "القاهرة",
                IsVerified = true
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                app.Logger.LogError("Account seed failed for {Email}: {Errors}",
                    email, string.Join(", ", created.Errors.Select(e => e.Description)));
                return null;
            }
        }

        foreach (var role in roles)
            await EnsureRoleAsync(user, role);

        return user;
    }

    await EnsureAccountAsync("salefnyapplication@gmail.com", "AdminSalefny@2026", "مدير النظام",
        new[] { AppRoles.Renter, AppRoles.Admin });

    await EnsureAccountAsync("salefny.app+superadmin@gmail.com", "Super@2026!", "مدير النظام العام",
        new[] { AppRoles.Renter, AppRoles.Admin, AppRoles.SuperAdmin });

    // Existing data created before roles existed is grandfathered in.
    const string demoOwnerId = "11111111-1111-1111-1111-111111111111";
    var allUsers = await db.Users.ToListAsync();
    foreach (var user in allUsers)
    {
        await EnsureRoleAsync(user, AppRoles.Renter);

        var hasListings = await db.Items.AnyAsync(i => i.OwnerId == user.Id);
        if (hasListings || user.Id == demoOwnerId)
            await EnsureRoleAsync(user, AppRoles.Owner);
    }
}

// 5. Configure the HTTP request pipeline
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "adminDashboard",
    pattern: "admin-dashboard",
    defaults: new { controller = "Admin", action = "Dashboard" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();