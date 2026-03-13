using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UserManagementApp.Data;
using UserManagementApp.Services;
using UserManagementApp.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1048576; // 1MB for longer comments
    });
builder.Services.AddSignalR();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var connectionStringRaw = Environment.GetEnvironmentVariable("DATABASE_URL")
                         ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionStringRaw) && (connectionStringRaw.Contains("postgres") || connectionStringRaw.Contains("postgresql")))
{
    Console.WriteLine("Applying PostgreSQL Database...");
    var pgConnectionString = UserManagementApp.ConnectionStringHelper.BuildPostgresConnectionString(connectionStringRaw);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(pgConnectionString));
}
else
{
    Console.WriteLine("Applying Local SQLite Database (Fallback)...");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=inventoryhub.db"));
}

// ASP.NET Core Identity setup
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Application Cookie to redirect to our Account controller
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthentication()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder";
})
.AddGitHub(githubOptions =>
{
    githubOptions.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "placeholder";
    githubOptions.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "placeholder";
    githubOptions.Scope.Add("user:email");
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Application services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICustomIdService, CustomIdService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Ensure the database is created and seed default admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    // Use EnsureCreated to guarantee tables exist without needing migration files 
    db.Database.EnsureCreated();
    
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
    }
    
    var adminEmail = "admin1@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new User 
        { 
            UserName = adminEmail, 
            Email = adminEmail, 
            Name = "Admin", 
            EmailConfirmed = true,
            Status = "active"
        };
        await userManager.CreateAsync(adminUser, "1234");
        // Also set the custom IsAdmin flag
        adminUser.IsAdmin = true;
        await userManager.UpdateAsync(adminUser);
    }
    else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// Configure Localization Middleware
var supportedCultures = new[] { "en-US", "ru-RU" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

localizationOptions.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider
{
    CookieName = "language"
});
app.UseRequestLocalization(localizationOptions);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();   // <-- must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapControllers();
app.MapBlazorHub();
app.MapHub<UserManagementApp.Hubs.DiscussionHub>("/discussionHub");
app.MapFallbackToPage("/_Host");

app.Run();
