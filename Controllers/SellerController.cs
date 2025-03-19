using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Propertease.Hubs;
using Propertease.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Propertease.Repos;
using PROPERTEASE.Models;
using Propertease.Services;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Propertease.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly EsewaService _esewaService;
        IWebHostEnvironment webHostEnvironment;
        private readonly ProperteaseDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext; // Inject SignalR Hub Context



        // Inject the ApplicationDbContext into the controller
        public SellerController(ProperteaseDbContext context, IWebHostEnvironment webHostEnvironment,
      IHubContext<NotificationHub> hubContext, INotificationService notificationService,
      EsewaService esewaService, IConfiguration _configuration)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            this._notificationService = notificationService;
            _esewaService = esewaService;
            this._configuration = _configuration;
        }

        // GET: Seller/AddProperty
        [HttpGet]
        public IActionResult AddProperty()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProperty(AddProperties addUserRequest)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get User ID from claims
            string? threeDModelFileName = null; // Define it outside so it's accessible later

            // Handle 3D model upload (only for House or Apartment)
            if (addUserRequest.ThreeDModel != null && (addUserRequest.PropertyType == "House" || addUserRequest.PropertyType == "Apartment"))
            {
                try
                {
                    string modelUploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "3DModels");
                    threeDModelFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(addUserRequest.ThreeDModel.FileName);
                    string modelFilePath = Path.Combine(modelUploadFolder, threeDModelFileName);

                    using (var modelStream = new FileStream(modelFilePath, FileMode.Create))
                    {
                        await addUserRequest.ThreeDModel.CopyToAsync(modelStream);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"3D model upload failed: {ex.Message}");
                    return View(addUserRequest);
                }
            }

            // Save base property details including new RoadAccess field
            var propertyModel = new Properties
            {
                PropertyType = addUserRequest.PropertyType,
                Title = addUserRequest.Title,
                Price = addUserRequest.Price,
                Description = addUserRequest.Description,
                District = addUserRequest.District,
                City = addUserRequest.City,
                Province = addUserRequest.Province,
                RoadAccess = addUserRequest.RoadAccess, // New field
                Latitude = addUserRequest.Latitude, // Add Latitude
                Longitude = addUserRequest.Longitude, // Add Longitude
                Status = "Pending",
                CreatedAt = DateTime.Now,
                ThreeDModel = threeDModelFileName, // Store the filename for reference

                SellerId = int.Parse(sellerId), // Automatically assign SellerId
            };

            await _context.properties.AddAsync(propertyModel);
            await _context.SaveChangesAsync(); // Save and get PropertyID

            if (addUserRequest.photo != null && addUserRequest.photo.Count > 0)
            {
                try
                {
                    List<PropertyImage> propertyImages = new List<PropertyImage>();
                    string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");

                    foreach (var photo in addUserRequest.photo) // Loop through each IFormFile
                    {
                        if (photo.Length > 0)
                        {
                            string photoFilename = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
                            string filePath = Path.Combine(uploadFolder, photoFilename);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await photo.CopyToAsync(fileStream);
                            }

                            propertyImages.Add(new PropertyImage
                            {
                                PropertyId = propertyModel.Id, // Foreign key
                                Photo = photoFilename
                            });
                        }
                    }
                    await _context.PropertyImages.AddRangeAsync(propertyImages);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"File upload failed: {ex.Message}");
                    return View(addUserRequest);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please upload a valid photo.");
                return View(addUserRequest);
            }

            // Save specific property type details with new fields
            if (addUserRequest.PropertyType == "House")
            {
                var house = new House
                {
                    Bedrooms = addUserRequest.Bedrooms,
                    Kitchens = addUserRequest.Kitchens,
                    SittingRooms = addUserRequest.SittingRooms,
                    Bathrooms = addUserRequest.Bathrooms,
                    Floors = addUserRequest.Floors,
                    LandArea = addUserRequest.LandArea,      // New: renamed Area to LandArea
                    BuildupArea = addUserRequest.BuildupArea,  // New field
                    BuiltYear = addUserRequest.BuiltYear,      // New field (assumed DateOnly or DateTime)
                    FacingDirection = addUserRequest.FacingDirection,
                   
                    PropertyID = propertyModel.Id // Foreign key
                };
                await _context.Houses.AddAsync(house);
            }
            else if (addUserRequest.PropertyType == "Apartment")
            {
                var apartment = new Apartment
                {
                    Rooms = addUserRequest.Rooms,
                    Kitchens = addUserRequest.Kitchens,
                    Bathrooms = addUserRequest.Bathrooms,
                    SittingRooms = addUserRequest.SittingRooms,
                    RoomSize = addUserRequest.RoomSize,
                    BuiltYear = addUserRequest.BuiltYear, // New field
                    PropertyID = propertyModel.Id // Foreign key
                };
                await _context.Apartments.AddAsync(apartment);
            }
            else if (addUserRequest.PropertyType == "Land")
            {
                var land = new Land
                {
                    LandArea = addUserRequest.LandArea,     // New: renamed Area to LandArea
                    LandType = addUserRequest.LandType,       // New field
                    SoilQuality = addUserRequest.SoilQuality, // New field
                    PropertyID = propertyModel.Id // Foreign key
                };
                await _context.Lands.AddAsync(land);
            }

            // Save specific property type details to the database
            await _context.SaveChangesAsync();

            var adminUsers = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();

            // Create notification for each admin
            foreach (var admin in adminUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    "New Property Listed",
                    $"A new property '{addUserRequest.Title}' has been listed and requires approval.",
                    "PropertyListed",
                    admin.Id,
                    propertyModel.Id
                );
            }

            return RedirectToAction("Listings");
        }

        public async Task<IActionResult> ViewPropertyDetails(int id)
        {
            // Get the base property with images
            var property = await _context.properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            // Get seller information
            var seller = await _context.Users.FindAsync(property.SellerId);

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
                PropertyType = property.PropertyType,
                Status = property.Status,
                RoadAccess = property.RoadAccess,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                ThreeDModel = property.ThreeDModel,

                // Seller information
                SellerName = seller?.FullName,
                SellerContact = seller?.ContactNumber,
                SellerImage = seller?.Image,

                // Images
                ImageUrl = property.PropertyImages?.Select(pi => pi.Photo).ToList() ?? new List<string>()
            };

            // Get property-specific details based on type
            if (property.PropertyType == "House")
            {
                var house = await _context.Houses.FirstOrDefaultAsync(h => h.PropertyID == id);
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
                    viewModel.BuildupArea = house.BuildupArea; // For compatibility
                }
            }
            else if (property.PropertyType == "Apartment")
            {
                var apartment = await _context.Apartments.FirstOrDefaultAsync(a => a.PropertyID == id);
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
                var land = await _context.Lands.FirstOrDefaultAsync(l => l.PropertyID == id);
                if (land != null)
                {
                    viewModel.LandArea = land.LandArea;
                    viewModel.LandType = land.LandType;
                    viewModel.SoilQuality = land.SoilQuality;
                }
            }

            return View(viewModel);
        }



        public async Task<IActionResult> Listings()
        {
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var properties = await _context.properties
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.PropertyImages)
                .ToListAsync();

            if (properties == null || !properties.Any())
            {
                properties = new List<Properties>();
            }

            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> EditProperty(int id)
        {
            var property = await _context.properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of this property
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (property.SellerId != sellerId)
            {
                return Forbid();
            }

            // Create the edit model with base property details
            var editModel = new AddProperties
            {
                PropertyId = property.Id,
                PropertyType = property.PropertyType,
                Title = property.Title,
                Description = property.Description,
                Price = property.Price,
                City = property.City,
                District = property.District,
                Province = property.Province,
                RoadAccess = property.RoadAccess,
                Latitude = property.Latitude,
                Longitude = property.Longitude
            };

            // Load property-specific details based on type
            if (property.PropertyType == "House")
            {
                var house = await _context.Houses.FirstOrDefaultAsync(h => h.PropertyID == id);
                if (house != null)
                {
                    editModel.Bedrooms = house.Bedrooms;
                    editModel.Kitchens = house.Kitchens;
                    editModel.SittingRooms = house.SittingRooms;
                    editModel.Bathrooms = house.Bathrooms;
                    editModel.Floors = house.Floors;
                    editModel.LandArea = house.LandArea;
                    editModel.BuildupArea = house.BuildupArea;
                    editModel.BuiltYear = house.BuiltYear;
                    editModel.FacingDirection = house.FacingDirection;
                }
            }
            else if (property.PropertyType == "Apartment")
            {
                var apartment = await _context.Apartments.FirstOrDefaultAsync(a => a.PropertyID == id);
                if (apartment != null)
                {
                    editModel.Rooms = apartment.Rooms;
                    editModel.Kitchens = apartment.Kitchens;
                    editModel.Bathrooms = apartment.Bathrooms;
                    editModel.SittingRooms = apartment.SittingRooms;
                    editModel.RoomSize = apartment.RoomSize;
                    editModel.BuiltYear = apartment.BuiltYear;
                }
            }
            else if (property.PropertyType == "Land")
            {
                var land = await _context.Lands.FirstOrDefaultAsync(l => l.PropertyID == id);
                if (land != null)
                {
                    editModel.LandArea = land.LandArea;
                    editModel.LandType = land.LandType;
                    editModel.SoilQuality = land.SoilQuality;
                }
            }

            // Pass existing images to the view
            ViewBag.ExistingImages = property.PropertyImages?.Select(pi => pi.Photo).ToList() ?? new List<string>();
            ViewBag.ExistingThreeDModel = property.ThreeDModel;

            return View(editModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditProperty(AddProperties model, List<string> ImagesToDelete)
        {
            if (model.PropertyId <= 0)
            {
                return BadRequest();
            }

            // Get the existing property
            var property = await _context.properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of this property
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (property.SellerId != sellerId)
            {
                return Forbid();
            }

            // Update basic property details
            property.Title = model.Title;
            property.Description = model.Description;
            property.Price = model.Price;
            property.City = model.City;
            property.District = model.District;
            property.Province = model.Province;
            property.RoadAccess = model.RoadAccess;
            property.Latitude = model.Latitude;
            property.Longitude = model.Longitude;

            // Handle 3D model upload if provided
            if (model.ThreeDModel != null && model.ThreeDModel.Length > 0 &&
                 (property.PropertyType == "House" || property.PropertyType == "Apartment"))
            { 
                // Delete old 3D model if exists
                if (!string.IsNullOrEmpty(property.ThreeDModel))
                {
                    string oldModelPath = Path.Combine(webHostEnvironment.WebRootPath, "3DModels", property.ThreeDModel);
                    if (System.IO.File.Exists(oldModelPath))
                    {
                        System.IO.File.Delete(oldModelPath);
                    }
                }

                // Save new 3D model
                string modelUploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "3DModels");
                string threeDModelFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ThreeDModel.FileName);
                string modelFilePath = Path.Combine(modelUploadFolder, threeDModelFileName);

                using (var modelStream = new FileStream(modelFilePath, FileMode.Create))
                {
                    await model.ThreeDModel.CopyToAsync(modelStream);
                }

                property.ThreeDModel = threeDModelFileName;
            }

            // Handle image deletions if any
            if (ImagesToDelete != null && ImagesToDelete.Any())
            {
                foreach (var imageToDelete in ImagesToDelete)
                {
                    // Remove from database
                    var imageEntity = property.PropertyImages.FirstOrDefault(pi => pi.Photo == imageToDelete);
                    if (imageEntity != null)
                    {
                        _context.PropertyImages.Remove(imageEntity);

                        // Delete physical file
                        string imagePath = Path.Combine(webHostEnvironment.WebRootPath, "Images", imageToDelete);
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                }
            }

            // Handle new image uploads if any
            if (model.photo != null && model.photo.Count > 0)
            {
                List<PropertyImage> newImages = new List<PropertyImage>();
                string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");

                foreach (var photo in model.photo)
                {
                    if (photo.Length > 0)
                    {
                        string photoFilename = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
                        string filePath = Path.Combine(uploadFolder, photoFilename);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(fileStream);
                        }

                        newImages.Add(new PropertyImage
                        {
                            PropertyId = property.Id,
                            Photo = photoFilename
                        });
                    }
                }

                if (newImages.Any())
                {
                    await _context.PropertyImages.AddRangeAsync(newImages);
                }
            }

            // Update property-specific details
            if (property.PropertyType == "House")
            {
                var house = await _context.Houses.FirstOrDefaultAsync(h => h.PropertyID == property.Id);
                if (house != null)
                {
                    house.Bedrooms = model.Bedrooms;
                    house.Kitchens = model.Kitchens;
                    house.SittingRooms = model.SittingRooms;
                    house.Bathrooms = model.Bathrooms;
                    house.Floors = model.Floors;
                    house.LandArea = model.LandArea;
                    house.BuildupArea = model.BuildupArea;
                    house.BuiltYear = model.BuiltYear;
                    house.FacingDirection = model.FacingDirection;
                }
            }
            else if (property.PropertyType == "Apartment")
            {
                var apartment = await _context.Apartments.FirstOrDefaultAsync(a => a.PropertyID == property.Id);
                if (apartment != null)
                {
                    apartment.Rooms = model.Rooms;
                    apartment.Kitchens = model.Kitchens;
                    apartment.Bathrooms = model.Bathrooms;
                    apartment.SittingRooms = model.SittingRooms;
                    apartment.RoomSize = model.RoomSize;
                    apartment.BuiltYear = model.BuiltYear;
                }
            }
            else if (property.PropertyType == "Land")
            {
                var land = await _context.Lands.FirstOrDefaultAsync(l => l.PropertyID == property.Id);
                if (land != null)
                {
                    land.LandArea = model.LandArea;
                    land.LandType = model.LandType;
                    land.SoilQuality = model.SoilQuality;
                }
            }

            // Save all changes to the database
            await _context.SaveChangesAsync();

            // Redirect to the property details page
            return RedirectToAction("ViewPropertyDetails", new { id = property.Id });
        }



        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.properties.FindAsync(id);
            if (property != null)
            {
                _context.properties.Remove(property);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Listings");
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var fullName = User.FindFirstValue("FullName");
                var firstName = fullName.Split(' ').FirstOrDefault();

                var properties = await _context.properties
                    .Where(p => p.SellerId == sellerId)
                    .ToListAsync();

                var totalListings = properties.Count;
                var pendingApprovals = properties.Count(p => p.Status == "Pending");
                var activeProperties = properties.Count(p => p.Status == "Approved");
                var newInquiries = properties.Count(p => p.Status == "Rejected");

                ViewBag.TotalListings = totalListings;
                ViewBag.PendingApprovals = pendingApprovals;
                ViewBag.ActiveProperties = activeProperties;
                ViewBag.NewInquiries = newInquiries;
                ViewBag.Name = firstName;

                return View(properties);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

        [HttpGet]
        // Modify the Boost action to accept a propertyId parameter
        public IActionResult Boost(int? propertyId = null)
        {
            var model = new BoostViewModel();

            // If propertyId is provided, pre-fill the property link
            if (propertyId.HasValue)
            {
                // Get the property to verify it exists and belongs to this seller
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var property = _context.properties
                    .FirstOrDefault(p => p.Id == propertyId && p.SellerId == userId);

                if (property != null)
                {
                    // Pre-fill the property link
                    model.PropertyLink = Url.Action("ViewPropertyDetails", "Seller", new { id = propertyId }, Request.Scheme);
                }
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Boost(BoostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Extract property ID from the URL
            int propertyId = 0;
            if (!string.IsNullOrEmpty(model.PropertyLink))
            {
                // Parse the property ID from the URL
                // Example URL: https://localhost:7152/Seller/ViewPropertyDetails/23
                var segments = model.PropertyLink.Split('/');
                var idSegment = segments.LastOrDefault();
                if (int.TryParse(idSegment, out int id))
                {
                    propertyId = id;
                }
            }

            // Verify the property exists and belongs to the current seller
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.SellerId == userId);

            if (property == null)
            {
                ModelState.AddModelError("PropertyLink", "Invalid property link or you don't own this property.");
                return View(model);
            }

            // Calculate the price
            model.CalculateTotalPrice();

            // Create a new boosted property entry
            var boostedProperty = new BoostedProperty
            {
                PropertyId = propertyId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Hours = model.SelectedHours,
                Price = model.TotalPrice,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(model.SelectedHours),
                IsActive = true
            };

            _context.BoostedProperties.Add(boostedProperty);
            await _context.SaveChangesAsync();

            // Redirect to a payment page or confirmation page
            return RedirectToAction("BoostConfirmation", new { id = boostedProperty.Id });
        }

        public IActionResult BoostConfirmation(int id)
        {
            var boostedProperty = _context.BoostedProperties
                .Include(bp => bp.Property)
                .FirstOrDefault(bp => bp.Id == id);

            if (boostedProperty == null)
            {
                return NotFound();
            }

            return View(boostedProperty);
        }
        [HttpGet]
        public IActionResult CalculatePrice(int hours, int people)
        {
            int basePrice = 100;
            int peopleCost = people * 10; // Changed from 2 to 10 Rs per person
            int additionalHours = (hours - 12) / 12 * 50; // Additional cost for every 12 hours
            int totalPrice = basePrice + peopleCost + additionalHours;

            return Json(totalPrice);
        }
        // Add this action to your SellerController
        // Modify the MyProperties action method in SellerController.cs
        // Add this to your MyProperties method in SellerController
        public async Task UpdateExpiredBoosts()
        {
            var currentTime = DateTime.UtcNow;
            var expiredBoosts = await _context.BoostedProperties
                .Where(bp => bp.IsActive && bp.EndTime <= currentTime)
                .ToListAsync();

            if (expiredBoosts.Any())
            {
                foreach (var boost in expiredBoosts)
                {
                    boost.IsActive = false;
                }
                await _context.SaveChangesAsync(); // Save changes to the database
            }
        }
        [HttpGet]
        public async Task<IActionResult> MyProperties()
        {
            // Update expired boosts
            await UpdateExpiredBoosts();

            // Get the current user's ID
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get all properties belonging to this seller
            var properties = await _context.properties
                .Include(p => p.PropertyImages)
                .Where(p => p.SellerId == userId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // Get boosted properties for this seller
            var boostedProperties = await _context.BoostedProperties
                .Include(bp => bp.Property)
                .Where(bp => bp.Property.SellerId == userId)
                .ToListAsync();

            ViewBag.BoostedProperties = boostedProperties;

            return View(properties);
        }
    }
}
