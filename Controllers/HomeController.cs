using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Propertease.Models;

namespace Propertease.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProperteaseDbContext _context;
        public HomeController(ILogger<HomeController> logger, ProperteaseDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Properties()
        {
            // Retrieve only approved properties from the database
            var approvedProperties = _context.properties
                                              .Where(p => p.Status == "Approved")
                                              .ToList();
            return View(approvedProperties);
        }



        [AllowAnonymous]
        public IActionResult Home()
        {
            return View();
        } public IActionResult About()
        {
            return View();
        }

        // Ensure that this action redirects to the login page for unauthorized users.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
           
                return RedirectToAction("Login", "User");
            

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
