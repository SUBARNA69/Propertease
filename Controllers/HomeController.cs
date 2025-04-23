using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Org.BouncyCastle.Tls;
using Propertease.Hubs;
using Propertease.Models;
using Propertease.Models.DTO;
using Propertease.Repos;
using Propertease.Services;

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
        //private readonly HousePricePredictionService _predictionService;
        private readonly PropertyRepository propertyRepository;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _clientFactory;


        public HomeController(ILogger<HomeController> logger, 
            ProperteaseDbContext context, 
            IWebHostEnvironment webHostEnvironment, 
            IHubContext<NotificationHub> _hubContext, 
            INotificationService _notificationService,
            //HousePricePredictionService predictionService,
            PropertyRepository propertyRepository,
            HttpClient httpClient,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            this._notificationService = _notificationService;
            //_predictionService = predictionService;
            this.propertyRepository = propertyRepository;
            _httpClient = httpClient;
            _clientFactory = clientFactory;
        }


        // To this if you want to keep the URL as /GetPredictedPrice/{id}
        //[Route("GetPredictedPrice/{id}")]
        //[HttpPost]
        //public async Task<IActionResult> GetPredictedPrice(int id)
        //{
        //    try
        //    {
        //        // Get property data from database using id
        //        var property = await _context.properties
        //            .FirstOrDefaultAsync(p => p.Id == id);

        //        if (property == null || property.PropertyType != "House")
        //        {
        //            return Json(new { success = false, message = "Property not found or not a house" });
        //        }

        //        // Get the house-specific details
        //        var house = await _context.Houses.FirstOrDefaultAsync(h => h.PropertyID == property.Id);

        //        if (house == null)
        //        {
        //            return Json(new { success = false, message = "House details not found" });
        //        }

        //        // Create prediction request from property data
        //        var predictionRequest = new HousePredictionRequest
        //        {
        //            // Map the data from your house object to the prediction request
        //            Bedrooms = house.Bedrooms ?? 0,
        //            Bathrooms = house.Bathrooms ?? 0,
        //            Floors = house.Floors ?? 0,
        //            LotArea = house.LandArea ?? 0.0,
        //            HouseArea = house.BuildupArea ?? 0.0,
        //            // Handle DateOnly conversion to year as int
        //            BuiltYear = house.BuiltYear
        //        };

        //        // Get prediction
        //        decimal predictedPrice = await _predictionService.PredictHousePrice(predictionRequest);

        //        return Json(new
        //        {
        //            success = true,
        //            predictedPrice,
        //            formattedPrice = String.Format("{0:C0}", predictedPrice) // C0 format for no decimal places
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log exception
        //        return Json(new { success = false, message = "Failed to get price prediction: " + ex.Message });
        //    }
        //}

        private async Task<decimal> PredictPriceAsync(HouseFeaturesDto features)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:5000"); // or your server IP

                var json = JsonConvert.SerializeObject(features);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/predict", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic prediction = JsonConvert.DeserializeObject(result);
                    return (decimal)prediction.predicted_price;
                }

                throw new Exception("Prediction service call failed.");
            }
        }

        public async Task<IActionResult> Properties(string propertyType = null, string district = null, string priceRange = null, List<string> amenities = null)
        {
            // Start with a query for approved and not sold properties
            var query = _context.properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Seller)
                .Where(p => p.Status == "Approved" && p.Status != "Sold" && p.IsDeleted==false);

            // Apply server-side filters if provided
            if (!string.IsNullOrEmpty(propertyType))
            {
                query = query.Where(p => p.PropertyType == propertyType);
            }

            if (!string.IsNullOrEmpty(district))
            {
                query = query.Where(p => p.District.Contains(district));
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                var parts = priceRange.Split('-');
                if (parts.Length == 2)
                {
                    if (decimal.TryParse(parts[0], out decimal minPrice))
                    {
                        query = query.Where(p => p.Price >= minPrice);
                    }

                    if (decimal.TryParse(parts[1], out decimal maxPrice))
                    {
                        query = query.Where(p => p.Price <= maxPrice);
                    }
                }
                else if (priceRange.EndsWith("+"))
                {
                    var minPriceStr = priceRange.TrimEnd('+');
                    if (decimal.TryParse(minPriceStr, out decimal minPrice))
                    {
                        query = query.Where(p => p.Price >= minPrice);
                    }
                }
            }

            // Execute the query to get the filtered properties
            var properties = await query.ToListAsync();

            // Create a list to hold our view models
            var propertyViewModels = new List<PropertyDetailsViewModel>();

            // Convert each property to a PropertyDetailsViewModel
            foreach (var property in properties)
            {
                var viewModel = new PropertyDetailsViewModel
                {
                    Id = property.Id,
                    Title = property.Title,
                    Price = property.Price,
                    Description = property.Description,
                    District = property.District,
                    City = property.City,
                    Province = property.Province,
                    ImageUrl = property.PropertyImages?.Select(img => "/Images/" + img.Photo).ToList() ?? new List<string>(),
                    SellerName = property.Seller?.FullName,
                    SellerContact = property.Seller?.ContactNumber,
                    SellerEmail = property.Seller?.Email,
                    SellerImage = property.Seller?.Image,
                    PropertyType = property.PropertyType,
                    Latitude = property.Latitude,
                    Longitude = property.Longitude,
                    RoadAccess = property.RoadAccess,
                    ThreeDModel = property.ThreeDModel
                };

                // Add property-specific details based on property type
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

                propertyViewModels.Add(viewModel);
            }
            // Get user's favorite property IDs and pass to the view
            List<int> favoritePropertyIds = new List<int>();
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                favoritePropertyIds = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.PropertyId)
                    .ToListAsync();
            }
            ViewBag.FavoritePropertyIds = favoritePropertyIds;

            // Get property type counts for statistics
            ViewBag.TotalProperties = propertyViewModels.Count;
            ViewBag.HouseCount = propertyViewModels.Count(p => p.PropertyType == "House");
            ViewBag.ApartmentCount = propertyViewModels.Count(p => p.PropertyType == "Apartment");
            ViewBag.LandCount = propertyViewModels.Count(p => p.PropertyType == "Land");

            // Get all districts for filter options
            ViewBag.Districts = await _context.properties
                .Where(p => p.Status == "Approved" && p.Status != "Sold")
                .Select(p => p.District)
                .Distinct()
                .ToListAsync();

            return View(propertyViewModels);
        }

        public async Task<IActionResult> BoostedProperties()
        {
            // Get the list of boosted property IDs
            var boostedPropertyIds = await _context.BoostedProperties
                .Where(bp => bp.IsActive)
                .Select(bp => bp.PropertyId)
                .ToListAsync();

            // Get all properties that are boosted, approved, and not sold
            var query = _context.properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Seller)
                .Where(p => boostedPropertyIds.Contains(p.Id) && p.Status == "Approved" && p.Status != "Sold");

            // Execute the query to get the boosted properties
            var boostedProperties = await query.ToListAsync();

            // Create a list to hold our view models
            var propertyViewModels = new List<PropertyDetailsViewModel>();

            // Convert each property to a PropertyDetailsViewModel
            foreach (var property in boostedProperties)
            {
                var viewModel = new PropertyDetailsViewModel
                {
                    Id = property.Id,
                    Title = property.Title,
                    Price = property.Price,
                    Description = property.Description,
                    District = property.District,
                    City = property.City,
                    Province = property.Province,
                    ImageUrl = property.PropertyImages?.Select(img => "/Images/" + img.Photo).ToList() ?? new List<string>(),
                    SellerName = property.Seller?.FullName,
                    SellerContact = property.Seller?.ContactNumber,
                    SellerEmail = property.Seller?.Email,
                    SellerImage = property.Seller?.Image,
                    PropertyType = property.PropertyType,
                    Latitude = property.Latitude,
                    Longitude = property.Longitude,
                    RoadAccess = property.RoadAccess,
                    ThreeDModel = property.ThreeDModel
                };
                // Add property-specific details based on property type
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

                propertyViewModels.Add(viewModel);
            }
            // Get user's favorite property IDs and pass to the view
            List<int> favoritePropertyIds = new List<int>();
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                favoritePropertyIds = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.PropertyId)
                    .ToListAsync();
            }
            ViewBag.FavoritePropertyIds = favoritePropertyIds;
            ViewBag.BoostedPropertyIds = boostedPropertyIds;

            // Get property type counts for statistics
            ViewBag.TotalBoostedProperties = propertyViewModels.Count;
            ViewBag.BoostedHouseCount = propertyViewModels.Count(p => p.PropertyType == "House");
            ViewBag.BoostedApartmentCount = propertyViewModels.Count(p => p.PropertyType == "Apartment");
            ViewBag.BoostedLandCount = propertyViewModels.Count(p => p.PropertyType == "Land");

            // Get all districts for filter options
            ViewBag.Districts = propertyViewModels
                .Select(p => p.District)
                .Distinct()
                .ToList();

            return View(propertyViewModels);
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

                    // Call prediction
                    var input = new HouseFeaturesDto
                    {
                        BuildupArea = house.BuildupArea,
                        Bedrooms = house.Bedrooms,
                        Bathrooms = house.Bathrooms,
                        BuiltYear = house.BuiltYear.Year, // Take only the year
                        LandArea = house.LandArea,
                        Floors = house.Floors
                    };

                    try
                    {
                        var predictedPrice = await PredictPriceAsync(input);
                        viewModel.PredictedPrice = predictedPrice;
                    }
                    catch (Exception ex)
                    {
                        viewModel.PredictedPrice = null;
                    }
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            var jsonData = JsonConvert.SerializeObject(new
                            {
                                Area = house.BuildupArea,
                                Bedrooms = house.Bedrooms,
                                BuiltYear = house.BuiltYear.Year,
                                Floors = house.Floors,
                                CurrentPropertyId = property.Id  // Added this line to exclude current property
                            });

                            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync("http://127.0.0.1:5000/recommend", content);

                            if (response.IsSuccessStatusCode)
                            {
                                var similarIds = JsonConvert.DeserializeObject<List<int>>(await response.Content.ReadAsStringAsync());

                                if (similarIds.Any())
                                {
                                    // Get the similar properties from the database
                                    var similarProperties = await _context.properties
                                        .Include(p => p.PropertyImages)
                                        .Where(p => similarIds.Contains(p.Id))
                                        .ToListAsync();

                                    // Map to view model
                                    viewModel.SimilarProperties = similarProperties.Select(p => new SimilarProperty
                                    {
                                        Id = p.Id,
                                        Title = p.Title,
                                        Price = p.Price,
                                        ImageUrl = p.PropertyImages.FirstOrDefault()?.Photo != null
                                            ? "/Images/" + p.PropertyImages.FirstOrDefault()?.Photo
                                            : "/placeholder.svg?height=150&width=150",
                                        District = p.District,
                                        City = p.City
                                    }).ToList();
                                }
                            }
                            else
                            {
                                _logger.LogError($"Failed to get similar properties. Status code: {response.StatusCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting similar properties: {0}", ex.Message);
                    }
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

                    // Apartment price prediction
                    using (var client = new HttpClient())
                    {
                        var jsonData = JsonConvert.SerializeObject(new
                        {
                            Rooms = apartment.Rooms,
                            Kitchens = apartment.Kitchens,
                            Bathrooms = apartment.Bathrooms,
                            SittingRooms = apartment.SittingRooms,
                            BuiltYear = apartment.BuiltYear.Year // Take only the year
                        });

                        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("http://127.0.0.1:5000/apredict/apartment", content);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                            viewModel.PredictedPrice = (decimal)result.predicted_price;
                        }
                    }
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
        
        // Controllers/HomeController.cs (or create a new controller)
        [HttpGet]
        // Add this method to your existing controller
        // Add this method to your existing controller
        public async Task<IActionResult> GetPropertyModel(int id)
        {
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null || string.IsNullOrEmpty(property.ThreeDModel))
            {
                return NotFound();
            }

            // Assuming 3D models are stored in a specific directory
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "3DModels", property.ThreeDModel);

            if (!System.IO.File.Exists(modelPath))
            {
                return NotFound();
            }

            // Return the file with the appropriate MIME type
            return PhysicalFile(modelPath, "model/gltf-binary");
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
            // Count verified users for "Happy Clients" statistic
            var verifiedUsersCount = await _context.Users
                .Where(u => u.IsEmailVerified == true && u.IsDeleted==false)
                .CountAsync();

            // Pass the verified users count to the view
            ViewBag.VerifiedUsersCount = verifiedUsersCount;

            // Get all approved properties that are not sold
            var allProperties = await _context.properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Seller)
                .Where(p => p.Status == "Approved" && p.Status != "Sold")
                .ToListAsync();

            // Create a list to hold our view models
            var propertyViewModels = new List<PropertyDetailsViewModel>();

            // Convert each property to a PropertyDetailsViewModel
            foreach (var property in allProperties)
            {
                var viewModel = new PropertyDetailsViewModel
                {
                    Id = property.Id,
                    Title = property.Title,
                    Price = property.Price,
                    Description = property.Description,
                    District = property.District,
                    City = property.City,
                    Province = property.Province,
                    ImageUrl = property.PropertyImages?.Select(img => "/Images/" + img.Photo).ToList() ?? new List<string>(),
                    SellerName = property.Seller?.FullName,
                    SellerContact = property.Seller?.ContactNumber,
                    SellerEmail = property.Seller?.Email,
                    SellerImage = property.Seller?.Image,
                    PropertyType = property.PropertyType,
                    Latitude = property.Latitude,
                    Longitude = property.Longitude,
                    RoadAccess = property.RoadAccess,
                    ThreeDModel = property.ThreeDModel
                };

                // Add property-specific details based on property type
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

                propertyViewModels.Add(viewModel);
            }

            // Reorder properties to show boosted ones first
            var orderedViewModels = propertyViewModels
                .OrderByDescending(p => boostedPropertyIds.Contains(p.Id)) // Boosted properties first
                .ThenByDescending(p => p.Id) // Then by newest
                .ToList();

            // Pass the boosted property IDs to the view
            ViewBag.BoostedPropertyIds = boostedPropertyIds;

            // Get user's favorite property IDs and pass to the view
            List<int> favoritePropertyIds = new List<int>();
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                favoritePropertyIds = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.PropertyId)
                    .ToListAsync();
            }
            ViewBag.FavoritePropertyIds = favoritePropertyIds;

            return View(orderedViewModels);
        }
        [HttpGet]
        // GET: Property/ModelViewer/5
        // Controllers/HomeController.cs - Add these methods to your existing controller

        public async Task<IActionResult> CompareProperties()
        {
            // Get properties from session with user-specific key
            string sessionKey = GetUserSpecificSessionKey("CompareProperties");
            var compareList = HttpContext.Session.GetString(sessionKey);
            var model = new ComparePropertyViewModel();

            if (!string.IsNullOrEmpty(compareList))
            {
                var propertyIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(compareList);

                // Fetch properties from database using your PropertyRepository
                foreach (var id in propertyIds)
                {
                    if (int.TryParse(id, out int propertyId))
                    {
                        var property = await propertyRepository.GetPropertyDetails(propertyId);
                        if (property != null)
                        {
                            model.Properties.Add(property);
                        }
                    }
                }
            }

            return View(model);
        }

        // Helper method to get user-specific session key
        private string GetUserSpecificSessionKey(string baseKey)
        {
            // If user is authenticated, append user ID to make the session key user-specific
            if (User.Identity.IsAuthenticated)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Assuming you're using ASP.NET Identity
                return $"{baseKey}_{userId}";
            }

            // For anonymous users, you could use a temporary ID stored in a cookie
            string anonymousId = Request.Cookies["AnonymousId"];
            if (string.IsNullOrEmpty(anonymousId))
            {
                anonymousId = Guid.NewGuid().ToString();
                Response.Cookies.Append("AnonymousId", anonymousId, new CookieOptions
                {
                    IsEssential = true,
                    Expires = DateTimeOffset.Now.AddDays(7)
                });
            }

            return $"{baseKey}_anonymous_{anonymousId}";
        }

        // Update all other methods to use the user-specific session key
        [HttpPost]
        public IActionResult AddToCompare(string id)
        {
            // Get user-specific session key
            string sessionKey = GetUserSpecificSessionKey("CompareProperties");

            // Get current compare list from session
            var compareList = HttpContext.Session.GetString(sessionKey);
            List<string> propertyIds = new List<string>();

            if (!string.IsNullOrEmpty(compareList))
            {
                propertyIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(compareList);
            }

            // Check if property is already in the list
            if (!propertyIds.Contains(id))
            {
                // Limit to 4 properties
                if (propertyIds.Count >= 4)
                {
                    return Json(new { success = false, message = "You can compare up to 4 properties at a time" });
                }

                propertyIds.Add(id);
            }

            // Save back to session with user-specific key
            HttpContext.Session.SetString(sessionKey, System.Text.Json.JsonSerializer.Serialize(propertyIds));

            return Json(new { success = true, count = propertyIds.Count });
        }

        [HttpPost]
        public IActionResult RemoveFromCompare(string id)
        {
            // Get user-specific session key
            string sessionKey = GetUserSpecificSessionKey("CompareProperties");

            // Get current compare list from session
            var compareList = HttpContext.Session.GetString(sessionKey);

            if (!string.IsNullOrEmpty(compareList))
            {
                var propertyIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(compareList);
                propertyIds.Remove(id);

                // Save back to session with user-specific key
                HttpContext.Session.SetString(sessionKey, System.Text.Json.JsonSerializer.Serialize(propertyIds));

                return Json(new { success = true, count = propertyIds.Count });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult ClearCompare()
        {
            // Get user-specific session key
            string sessionKey = GetUserSpecificSessionKey("CompareProperties");

            // Remove the user-specific session data
            HttpContext.Session.Remove(sessionKey);
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetCompareList()
        {
            // Get user-specific session key
            string sessionKey = GetUserSpecificSessionKey("CompareProperties");

            var compareList = HttpContext.Session.GetString(sessionKey);
            List<string> propertyIds = new List<string>();

            if (!string.IsNullOrEmpty(compareList))
            {
                propertyIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(compareList);
            }

            return Json(new { properties = propertyIds, count = propertyIds.Count });
        }
        // Add to HomeController.cs
        [HttpGet]
        
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
        [AllowAnonymous]
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
            // Check if property is already booked
            var isPropertyBooked = await _context.PropertyViewingRequests
                .AnyAsync(r => r.PropertyId == request.PropertyId && r.Status == "Approved");

            if (isPropertyBooked)
            {
                TempData["ErrorMessage"] = "This property is already booked for viewing.";
                return RedirectToAction("Details", new { id = request.PropertyId });
            }

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
            // Set initial status to Pending
            request.Status = "Pending";

            try
            {
                // Add to database
                _context.PropertyViewingRequests.Add(request);
                await _context.SaveChangesAsync();

                // Get property details for notification
                var property = await _context.properties
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

                if (property != null)
                {
                    // Send notification to seller
                    await _notificationService.CreateNotificationAsync(
                        "New Viewing Request",
                        $"A new viewing request has been submitted for your property '{property.Title}'.",
                        "ViewingRequest",
                        property.SellerId,
                        property.Id
                    );
                }

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

            [HttpGet]
        public async Task<IActionResult> Forum()
        {
            // Fetch all forum posts with their related user and comments
            var posts = await _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)  // Include the user for each comment
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Create a new view model with an empty forum post for the form
            var viewModel = new ForumViewModel
            {
                ForumPost = new ForumPost(),
                Posts = posts
            };

            return View(viewModel);
        }

        // POST: /Forum (Create a new post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forum([Bind("Title,Content,AudioFile")] ForumPost forumPost)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return NotFound("User ID not found.");
                }

                forumPost.UserId = int.Parse(userId);
                forumPost.CreatedAt = DateTime.UtcNow;

                // If an audio recording is attached, process it.
                if (!string.IsNullOrEmpty(forumPost.AudioFile))
                {
                    try
                    {
                        // The AudioFile property holds a Base64 string (e.g., "data:audio/wav;base64,AAAA...")
                        var base64Data = forumPost.AudioFile.Split(',')[1];
                        var audioBytes = Convert.FromBase64String(base64Data);
                        var fileName = $"audio_{Guid.NewGuid()}.wav";
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                        // Ensure the uploads folder exists.
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, audioBytes);

                        // Save only the file name (or relative path) in the database.
                        forumPost.AudioFile = fileName;

                        // Debug information
                        Console.WriteLine($"Audio file saved as: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        Console.WriteLine($"Error saving audio file: {ex.Message}");
                        // Continue without the audio file
                        forumPost.AudioFile = null;
                    }
                }

                _context.ForumPosts.Add(forumPost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Forum));
            }

            // If model state is invalid, re-fetch posts to display on the view.
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


        // POST: /Forum/AddForumComment (Add a comment to a post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddForumComment(int postId, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest("Comment content cannot be empty.");
            }

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

        // POST: /Forum/DeleteForumComment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForumComment(int id)
        {
            var comment = await _context.ForumComments
                .Include(c => c.ForumPost)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound("Comment not found.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("User ID not found.");
            }

            var currentUserId = int.Parse(userId);

            // Allow deletion only if the current user is the comment owner or the post owner
            if (comment.UserId != currentUserId && comment.ForumPost.UserId != currentUserId)
            {
                return Forbid("You are not authorized to delete this comment.");
            }

            _context.ForumComments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Forum));
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
                .Where(r => r.BuyerId == buyerId && r.Status == "Completed" && r.Properties.IsDeleted == false)
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
                PurchasedDate = v.Properties.SoldDate, // Add the SoldDate from Properties
                HasBeenRated = ratedPropertyIds.Contains(v.PropertyId)
            }).ToList();

            return View(viewModel);
        }

        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> MyBuyerRatings()
        {
            var buyerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var ratings = await _context.BuyerRatings // Using UserRatings since that's what's in your DbContext
                .Include(r => r.Seller)
                .Include(r => r.Property)
                .Where(r => r.BuyerId == buyerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Calculate average rating
            double averageRating = 0;
            if (ratings.Any())
            {
                averageRating = ratings.Average(r => r.Rating);
            }

            ViewBag.AverageRating = averageRating;
            ViewBag.TotalRatings = ratings.Count;

            return View(ratings);
        }
        // Add this method to the HomeController class to track property views
        //private async Task TrackPropertyView(int propertyId)
        //{
        //    // Only track views for authenticated users
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        //        // Create a new property view record
        //        var propertyView = new PropertyView
        //        {
        //            UserId = userId,
        //            PropertyId = propertyId,
        //            ViewedAt = DateTime.Now
        //        };

        //        // Add to database
        //        _context.PropertyViews.Add(propertyView);
        //        await _context.SaveChangesAsync();
        //    }
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int propertyId)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Please login to add favorites" });
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);

                if (existingFavorite != null)
                {
                    _context.Favorites.Remove(existingFavorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isFavorite = false });
                }
                else
                {
                    var favorite = new Favorite
                    {
                        UserId = userId,
                        PropertyId = propertyId,
                        DateAdded = DateTime.UtcNow
                    };
                    _context.Favorites.Add(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isFavorite = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for property {PropertyId}", propertyId);
                return Json(new { success = false, message = "Error updating favorites. Please try again." });
            }
        }


        [AllowAnonymous]
        public async Task<IActionResult> GetFavorites()
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get user's favorite property IDs
            var favoriteIds = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.PropertyId)
                .ToListAsync();

            return Json(new { success = true, favorites = favoriteIds });
        }

        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> Favorites()
        {
            // Get the current user's ID
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get user's favorite properties
            var favoriteProperties = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Join(_context.properties,
                    f => f.PropertyId,
                    p => p.Id,
                    (f, p) => p)
                .Include(p => p.PropertyImages)
                .Include(p => p.Seller)
                .Where(p => p.Status == "Approved" && p.Status != "Sold")
                .ToListAsync();

            // Create a list to hold our view models
            var propertyViewModels = new List<PropertyDetailsViewModel>();

            // Convert each property to a PropertyDetailsViewModel
            foreach (var property in favoriteProperties)
            {
                var viewModel = new PropertyDetailsViewModel
                {
                    Id = property.Id,
                    Title = property.Title,
                    Price = property.Price,
                    Description = property.Description,
                    District = property.District,
                    City = property.City,
                    Province = property.Province,
                    ImageUrl = property.PropertyImages?.Select(img => "/Images/" + img.Photo).ToList() ?? new List<string>(),
                    SellerName = property.Seller?.FullName,
                    SellerContact = property.Seller?.ContactNumber,
                    SellerEmail = property.Seller?.Email,
                    SellerImage = property.Seller?.Image,
                    PropertyType = property.PropertyType,
                    Latitude = property.Latitude,
                    Longitude = property.Longitude,
                    RoadAccess = property.RoadAccess,
                    ThreeDModel = property.ThreeDModel
                };

                // Add property-specific details based on property type
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

                propertyViewModels.Add(viewModel);
            }

            // Set all properties as favorites for the view
            ViewBag.FavoritePropertyIds = favoriteProperties.Select(p => p.Id).ToList();

            return View(propertyViewModels);
        }


    }

}
