using Propertease.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Propertease.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
namespace Propertease
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<PropertyRepository>();
            builder.Services.AddDbContext<ProperteaseDbContext>(options =>
              options.UseSqlServer(builder.Configuration.GetConnectionString("dbConn"))
                     .EnableSensitiveDataLogging());

            builder.Services.AddSingleton<ProperteaseSecurityProvider>();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/Users/Login";
                    o.LogoutPath = "/User/Logout";
                });

            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(1);
                o.Cookie.HttpOnly = true;
            });
            builder.Services.AddControllersWithViews();
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseStaticFiles();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Home}/{id?}");

            app.Run();
        }
    }
}
