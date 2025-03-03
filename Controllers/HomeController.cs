using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Propertease.Models;

namespace Propertease.Controllers
{
    [Authorize(Roles ="Buyer")]
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
            var properties = _context.properties
                .Include(p => p.PropertyImages) // Ensure PropertyImages are loaded
                .Where(p => p.Status == "Approved") // Filter only approved properties
                .OrderByDescending(p => p.Id) // Order by latest
                .Take(3) // Get only 3 properties
                .ToList();

            return View(properties);
        }
         public IActionResult About()
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
        public IActionResult Homes()
        {
            return View();
        }

        public IActionResult Apartments()
        {
            return View();
        }

        public IActionResult Lands()
        {
            return View();
        }
        public IActionResult Forum()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forum([Bind("Title,Content")] ForumPost forumPost)
        {
            if (ModelState.IsValid)
            {
                // Get the UserId from the user's claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return NotFound("User ID not found in claims.");
                }

                // Get the FullName from the user's claims (if needed)
                var fullName = User.FindFirstValue("FullName"); // Replace "FullName" with the actual claim type

                // Set the UserId of the ForumPost
                forumPost.UserId = int.Parse(userId);

                // Optionally, you can set the User object if needed
                // forumPost.User = await _context.Users.FindAsync(int.Parse(userId));

                // Add the ForumPost to the database
                _context.ForumPosts.Add(forumPost);
                await _context.SaveChangesAsync();

                // Redirect to a success page or the post details page
                return RedirectToAction(nameof(Forum)); // Replace with your desired action
            }

            // If the model state is invalid, return to the form with validation errors
            return View(forumPost);
        }
    }
}
