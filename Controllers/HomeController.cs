using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Propertease.Models;
using Propertease.Repos;

namespace Propertease.Controllers
{
    [Authorize(Roles ="Buyer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProperteaseDbContext _context;
        IWebHostEnvironment webHostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public HomeController(ILogger<HomeController> logger, ProperteaseDbContext context, IWebHostEnvironment webHostEnvironment, IHubContext<NotificationHub> _hubContext, INotificationService _notificationService)
        {
            _logger = logger;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this._notificationService = _notificationService;


        }

        public IActionResult Properties()
        {
            // Retrieve only approved and not sold properties from the database
            var availableProperties = _context.properties
                                              .Include(p => p.PropertyImages)
                                              .Where(p => p.Status == "Approved" && p.Status != "Sold")
                                              .ToList();
            return View(availableProperties);
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

            // If the property is sold, redirect to a "Property Sold" page or show a message
            if (property.Status == "Sold")
            {
                TempData["ErrorMessage"] = "This property is no longer available as it has been sold.";
                return RedirectToAction("Properties");
            }

            // Get seller ratings
            var sellerRatings = await _context.SellerRatings
                .Include(r => r.Buyer)
                .Where(r => r.SellerId == property.SellerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            double averageRating = 0;
            if (sellerRatings.Any())
            {
                averageRating = sellerRatings.Average(r => r.Rating);
            }

            // Get seller's past properties (including current one)
            var sellerProperties = await _context.properties
                .Include(p => p.PropertyImages)
                .Where(p => p.SellerId == property.SellerId && p.Id != property.Id) // Exclude current property
                .OrderByDescending(p => p.CreatedAt)
                .Take(4) // Limit to 4 past properties
                .ToListAsync();

            // Create the view model
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
                SellerEmail = property.Seller.Email,
                SellerImage = property.Seller.Image != null ? property.Seller.Image : null,
                CreatedAt = property.Seller.CreatedAt,
                PropertyType = property.PropertyType,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                RoadAccess = property.RoadAccess,
                ThreeDModel = property.ThreeDModel != null ? "/3DModels/" + property.ThreeDModel : null,

                // Add seller rating information
                SellerAverageRating = averageRating,
                SellerTotalRatings = sellerRatings.Count,

                // Add seller reviews
                SellerReviews = sellerRatings.Take(3).Select(r => new SellerReview
                {
                    Id = r.Id,
                    BuyerName = r.Buyer?.FullName ?? "Anonymous",
                    Rating = r.Rating,
                    Review = r.Review,
                    CreatedAt = r.CreatedAt
                }).ToList(),

                // Add seller past properties
                SellerPastProperties = sellerProperties.Select(p => new SellerPastProperty
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    ImageUrl = p.PropertyImages.FirstOrDefault()?.Photo != null
                        ? "/Images/" + p.PropertyImages.FirstOrDefault()?.Photo
                        : "/placeholder.svg?height=150&width=150",
                    Status = p.Status,
                    ListedDate = p.CreatedAt
                }).ToList()
            };

            // Add property-specific details based on type
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
        public async Task<IActionResult> Home()
        {
            // Get active boosted properties that are not sold
            var currentTime = DateTime.UtcNow;
            var boostedPropertyIds = await _context.BoostedProperties
                .Where(bp => bp.IsActive && bp.StartTime <= currentTime && bp.EndTime >= currentTime)
                .Join(_context.properties,
                      bp => bp.PropertyId,
                      p => p.Id,
                      (bp, p) => new { BoostedProperty = bp, Property = p })
                .Where(x => x.Property.Status != "Sold")
                .Select(x => x.BoostedProperty.PropertyId)
                .ToListAsync();

            // Get all approved properties that are not sold
            var allProperties = await _context.properties
                .Include(p => p.PropertyImages)
                .Where(p => p.Status == "Approved" && p.Status != "Sold")
                .ToListAsync();

            // Reorder properties to show boosted ones first
            var orderedProperties = allProperties
                .OrderByDescending(p => boostedPropertyIds.Contains(p.Id)) // Boosted properties first
                .ThenByDescending(p => p.Id) // Then by newest
                .ToList();

            // Pass the boosted property IDs to the view
            ViewBag.BoostedPropertyIds = boostedPropertyIds;

            return View(orderedProperties);
        }
        // GET: Property/ModelViewer/5
        [HttpGet]
        // GET: Property/ModelViewer/5
        public IActionResult ModelViewer(int id)
        {
            // Get the property details
            var property = _context.properties
                .Include(p => p.Seller)
                .FirstOrDefault(p => p.Id == id);

            if (property == null || string.IsNullOrEmpty(property.ThreeDModel))
            {
                return NotFound();
            }

            // Verify the file exists
            var modelPath = Path.Combine(webHostEnvironment.WebRootPath, "3DModels", property.ThreeDModel);
            if (!System.IO.File.Exists(modelPath))
            {
                // Log the error and return a meaningful message
                _logger.LogError($"3D model file not found at: {modelPath}");
                return View("Error", new ErrorViewModel
                {
                    Message = "The 3D model file for this property is currently unavailable."
                });
            }

            // Map to view model
            var viewModel = new PropertyDetailsViewModel
            {
                Id = property.Id,
                Title = property.Title,
                ThreeDModel = property.ThreeDModel, // This should be just the filename, not the full path
                                                    // Map other properties as needed
            };

            return View(viewModel);
        }

        // Add an endpoint to serve the 3D model file
        [HttpGet]
        [Route("3DModels/{filename}")]
        public IActionResult GetModelFile(string filename)
        {
            // Sanitize the filename to prevent directory traversal
            filename = Path.GetFileName(filename);

            var modelPath = Path.Combine(webHostEnvironment.WebRootPath, "3DModels", filename);
            if (!System.IO.File.Exists(modelPath))
            {
                return NotFound();
            }

            // Serve the file with the correct MIME type
            return PhysicalFile(modelPath, "model/gltf-binary");
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
        [HttpGet]
        public IActionResult GetBuyerDetails()
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false });
            }

            // Get the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false });
            }

            // Get buyer details from database
            var buyer = _context.Users
                .FirstOrDefault(u => u.Id == int.Parse(userId));

            if (buyer == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                name = buyer.FullName,
                email = buyer.Email,
                contactNumber = buyer.ContactNumber
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleViewing(PropertyViewingRequest request)
        {
            // Set BuyerId if user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int buyerId))
                {
                    request.BuyerId = buyerId;
                }
                else
                {
                    request.BuyerId = 0; // Default value
                }
            }
            else
            {
                request.BuyerId = 0; // Default value for non-authenticated users
            }

            // Set RequestedAt to current time
            request.RequestedAt = DateTime.UtcNow;

            try
            {
                // Add to database
                _context.PropertyViewingRequests.Add(request);
                await _context.SaveChangesAsync();

                // Redirect to success page or back to details
                TempData["SuccessMessage"] = "Viewing request submitted successfully!";
                return RedirectToAction("Details", new { id = request.PropertyId });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error scheduling property viewing");

                // Add error to ModelState
                ModelState.AddModelError("", "An error occurred while scheduling the viewing. Please try again.");

                // Redirect back to details with error
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Details", new { id = request.PropertyId });
            }
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
        // ... existing code ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddForumComment(int postId, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest("Comment content cannot be empty.");
            }

            // Get the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("User ID not found in claims.");
            }

            var comment = new ForumComment
            {
                Content = content,
                ForumPostId = postId,
                UserId = int.Parse(userId),
                CreatedAt = DateTime.UtcNow
            };

            _context.ForumComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Forum));
        }

        // ... existing code ...
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


        [Authorize(Roles = "Buyer")]
        [HttpGet]
        public async Task<IActionResult> RateSeller(int propertyId, int? requestId = null)
        {
            // Get the property
            var property = await _context.properties
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null)
            {
                return NotFound();
            }

            // Check if the property is sold
            if (property.Status != "Sold")
            {
                TempData["ErrorMessage"] = "You can only rate sellers for properties that have been sold.";
                return RedirectToAction("Properties");
            }

            // Check if the buyer has already rated this property
            var buyerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existingRating = await _context.SellerRatings
                .FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.BuyerId == buyerId);

            if (existingRating != null)
            {
                TempData["ErrorMessage"] = "You have already rated this seller for this property.";
                return RedirectToAction("Properties");
            }

            // Create the view model
            var viewModel = new SellerRatingViewModel
            {
                PropertyId = propertyId,
                PropertyTitle = property.Title,
                SellerId = property.SellerId,
                SellerName = property.Seller?.FullName,
                ViewingRequestId = requestId
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Buyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateSeller(SellerRatingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the buyer ID
            var buyerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Create the rating
            var rating = new SellerRating
            {
                SellerId = model.SellerId,
                BuyerId = buyerId,
                PropertyId = model.PropertyId,
                Rating = model.Rating,
                Review = model.Review,
                ViewingRequestId = model.ViewingRequestId,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            _context.SellerRatings.Add(rating);
            await _context.SaveChangesAsync();

            // Notify the seller
            await _notificationService.CreateNotificationAsync(
                "New Rating Received",
                $"You received a {model.Rating}-star rating for property '{model.PropertyTitle}'.",
                "NewRating",
                model.SellerId,
                model.PropertyId
            );

            TempData["SuccessMessage"] = "Thank you for your rating and review!";
            return RedirectToAction("Properties");
        }

        [Authorize(Roles = "Buyer")]
    
        public async Task<IActionResult> MyPurchases()
        {
            var buyerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get all viewing requests for this buyer that are marked as "Completed" (property was sold)
            var completedViewings = await _context.PropertyViewingRequests
                .Include(r => r.Properties)
                    .ThenInclude(p => p.Seller)
                .Include(r => r.Properties)
                    .ThenInclude(p => p.PropertyImages)
                .Where(r => r.BuyerId == buyerId && r.Status == "Completed")
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            // Check which properties have already been rated by this buyer
            var ratedPropertyIds = await _context.SellerRatings
                .Where(r => r.BuyerId == buyerId)
                .Select(r => r.PropertyId)
                .ToListAsync();

            // Create view model
            var viewModel = completedViewings.Select(v => new PurchasedPropertyViewModel
            {
                ViewingRequestId = v.Id,
                PropertyId = v.PropertyId,
                PropertyTitle = v.Properties.Title,
                PropertyDescription = v.Properties.Description,
                PropertyType = v.Properties.PropertyType,
                PropertyPrice = v.Properties.Price,
                PropertyLocation = $"{v.Properties.District}, {v.Properties.City}, {v.Properties.Province}",
                PropertyImage = v.Properties.PropertyImages.FirstOrDefault()?.Photo != null
                    ? "/Images/" + v.Properties.PropertyImages.FirstOrDefault()?.Photo
                    : "/placeholder.svg?height=200&width=300",
                SellerId = v.Properties.SellerId,
                SellerName = v.Properties.Seller?.FullName,
                ViewingDate = v.ViewingDate,
                HasBeenRated = ratedPropertyIds.Contains(v.PropertyId)
            }).ToList();

            return View(viewModel);
        }

        // Add this method to the HomeController class to track property views
        private async Task TrackPropertyView(int propertyId)
        {
            // Only track views for authenticated users
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Create a new property view record
                var propertyView = new PropertyView
                {
                    UserId = userId,
                    PropertyId = propertyId,
                    ViewedAt = DateTime.Now
                };

                // Add to database
                _context.PropertyViews.Add(propertyView);
                await _context.SaveChangesAsync();
            }
        }

    }

}
