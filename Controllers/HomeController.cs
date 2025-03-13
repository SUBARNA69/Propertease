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
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.properties
                .Include(p => p.Seller)
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var viewModel = new PropertyDetailsViewModel
            {
                Id = property.Id,
                Title = property.Title,
                Price = property.Price,
                Description = property.Description,
                District = property.District,
                City = property.City,
                Province = property.Province,
                ImageUrl = property.PropertyImages.Select(img => "/Images/" + img.Photo).ToList(),
                SellerName = property.Seller.FullName,
                SellerContact = property.Seller.ContactNumber,
                SellerImage = property.Seller.Image != null ? "/ProfileImages/" + property.Seller.Image : "/Images/default-profile.png", // Add seller image
                PropertyType = property.PropertyType,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                RoadAccess = property.RoadAccess,
                ThreeDModel = property.ThreeDModel != null ? "/3DModels/" + property.ThreeDModel : null // Add 3D Model URL
            };

            if (property.PropertyType == "House")
            {
                var house = await _context.Houses.FirstOrDefaultAsync(h => h.PropertyID == property.Id);
                if (house != null)
                {
                    viewModel.Bedrooms = house.Bedrooms;
                    viewModel.Kitchens = house.Kitchens;
                    viewModel.SittingRooms = house.SittingRooms;
                    viewModel.Bathrooms = house.Bathrooms;
                    viewModel.Floors = house.Floors;
                    viewModel.LandArea = house.LandArea;
                    viewModel.BuildupArea = house.BuildupArea;
                    viewModel.BuiltYear = house.BuiltYear;
                    viewModel.FacingDirection = house.FacingDirection;
                }
            }
            else if (property.PropertyType == "Apartment")
            {
                var apartment = await _context.Apartments.FirstOrDefaultAsync(a => a.PropertyID == property.Id);
                if (apartment != null)
                {
                    viewModel.Rooms = apartment.Rooms;
                    viewModel.Kitchens = apartment.Kitchens;
                    viewModel.Bathrooms = apartment.Bathrooms;
                    viewModel.SittingRooms = apartment.SittingRooms;
                    viewModel.RoomSize = apartment.RoomSize;
                    viewModel.BuiltYear = apartment.BuiltYear;
                }
            }
            else if (property.PropertyType == "Land")
            {
                var land = await _context.Lands.FirstOrDefaultAsync(l => l.PropertyID == property.Id);
                if (land != null)
                {
                    viewModel.LandArea = land.LandArea;
                    viewModel.LandType = land.LandType;
                    viewModel.SoilQuality = land.SoilQuality;
                }
            }

            return View(viewModel);
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
        public async Task<IActionResult> Homes()
        {
            // Retrieve only approved properties of type "House"
            var houses = await _context.properties
                .Include(p => p.PropertyImages) // Include property images
                .Where(p => p.Status == "Approved" && p.PropertyType == "House")
                .OrderByDescending(p => p.Id) // Order by latest
                .ToListAsync();

            return View(houses);
        }

        public async Task<IActionResult> Apartments()
        {
            // Retrieve only approved properties of type "Apartment"
            var apartments = await _context.properties
                .Include(p => p.PropertyImages) // Include property images
                .Where(p => p.Status == "Approved" && p.PropertyType == "Apartment")
                .OrderByDescending(p => p.Id) // Order by latest
                .ToListAsync();

            return View(apartments);
        }

        public async Task<IActionResult> Lands()
        {
            // Retrieve only approved properties of type "Land"
            var lands = await _context.properties
                .Include(p => p.PropertyImages) // Include property images
                .Where(p => p.Status == "Approved" && p.PropertyType == "Land")
                .OrderByDescending(p => p.Id) // Order by latest
                .ToListAsync();

            return View(lands);
        }

        public async Task<IActionResult> Forum()
        {
            // Fetch all forum posts with user and comment information
            var posts = await _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModel = new ForumViewModel
            {
                ForumPost = new ForumPost(),
                Posts = posts
            };

            return View(viewModel);
        }

        // POST: /Forum
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

                // Set the UserId of the ForumPost
                forumPost.UserId = int.Parse(userId);
                forumPost.CreatedAt = DateTime.UtcNow;

                // Add the ForumPost to the database
                _context.ForumPosts.Add(forumPost);
                await _context.SaveChangesAsync();

                // Redirect back to the forum page
                return RedirectToAction(nameof(Forum));
            }

            // If the model state is invalid, fetch posts again and return the full view model
            var posts = await _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModel = new ForumViewModel
            {
                ForumPost = forumPost,
                Posts = posts
            };

            return View(viewModel);
        }
    }
}
