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
using PROPERTEASE.Services;
using OfficeOpenXml;
using System.Threading.RateLimiting;

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
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("MessagePolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers["X-Client-Id"],
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});
// Register other services
// Add this before building the host
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // For non-commercial use
// Or use LicenseContext.Commercial if you have a commercial license
builder.Services.AddScoped<PropertyRepository>();
// Add this line where your other services are registered
builder.Services.AddScoped<INotificationService, NotificationService>(); 
builder.Services.AddDbContext<ProperteaseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbConn"))
           .EnableSensitiveDataLogging());
builder.Services.AddSingleton<AwsSnsService>();
builder.Services.AddTransient<EmailService>();
builder.Services.AddScoped<HousePricePredictionService>();

// Add this to your services configuration
builder.Services.AddSingleton<HousePricePredictionService>();
// Add this to your ConfigureServices method
builder.Services.AddHostedService<BoostedPropertyCleanupService>();
// Add this line with your other SignalR configuration
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>(); 
builder.Services.AddHttpClient<EsewaPaymentService>();
builder.Services.AddScoped<EsewaPaymentService>();
builder.Services.AddSingleton<SmsService>();
builder.Services.AddSingleton<ProperteaseSecurityProvider>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
// In Program.cs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/User/Login";
        o.LogoutPath = "/User/Logout";
        o.AccessDeniedPath = "/User/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(1);
        o.SlidingExpiration = true;
    });
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(1);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;

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

app.MapHub<NotificationHub>("/notificationHub");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Home}/{id?}");

app.Run();
