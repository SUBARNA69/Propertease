using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Propertease.Hubs;
using Propertease.Repos;
using Propertease.Repos.Propertease.Services;
using Propertease.Security;
using Propertease.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Propertease.Services;

var builder = WebApplication.CreateBuilder(args);

// Register CORS policy (allowing all origins for testing purposes)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition"); // Useful for file downloads
    });
});

// Register other services
builder.Services.AddScoped<PropertyRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddDbContext<ProperteaseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbConn"))
           .EnableSensitiveDataLogging());
builder.Services.AddSingleton<AwsSnsService>();
builder.Services.AddTransient<EmailService>();
// Add this to your ConfigureServices method
builder.Services.AddHostedService<BoostedPropertyCleanupService>();
// Add this line with your other SignalR configuration
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddSingleton<SmsService>();
builder.Services.AddSingleton<ProperteaseSecurityProvider>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Helpful during development
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/User/Login";
        o.LogoutPath = "/User/Logout";
        o.AccessDeniedPath = "/User/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        o.SlidingExpiration = true;
    });
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(1);
    o.Cookie.HttpOnly = true;
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

// Use the CORS policy
app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hubs after authentication
app.MapHub<NotificationHub>("notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Home}/{id?}");

app.Run();
