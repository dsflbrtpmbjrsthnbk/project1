using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                         ?? builder.Configuration.GetConnectionString("DefaultConnection");

var connectionString = UserManagementApp.ConnectionStringHelper
    .BuildPostgresConnectionString(rawConnectionString);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google"; // Default to Google for external challenge
})
.AddCookie("Cookies")
.AddGoogle(googleOptions => {
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder";
})
.AddFacebook(facebookOptions => {
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "placeholder";
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "placeholder";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<UserManagementApp.Services.IEmailService, UserManagementApp.Services.EmailService>();
builder.Services.AddScoped<UserManagementApp.Services.ICustomIdService, UserManagementApp.Services.CustomIdService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var supportedCultures = new[] { "en-US", "uz-UZ" }; // English and Uzbek (example)
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapHub<UserManagementApp.Hubs.DiscussionHub>("/discussionHub");
app.MapFallbackToPage("/_Host");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
