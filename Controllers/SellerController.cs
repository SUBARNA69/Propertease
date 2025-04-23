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
using PROPERTEASE.Services;
using payment_gateway_nepal;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Propertease.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly ILogger<SellerController> _logger;
        private readonly IConfiguration _configuration;
        private readonly EsewaPaymentService _esewaService;
        IWebHostEnvironment webHostEnvironment;
        private readonly ProperteaseDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext; // Inject SignalR Hub Context



        // Inject the ApplicationDbContext into the controller
        public SellerController(ProperteaseDbContext context, IWebHostEnvironment webHostEnvironment,
      IHubContext<NotificationHub> hubContext, INotificationService notificationService,
      EsewaPaymentService esewaService, IConfiguration _configuration, ILogger<SellerController> logger)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            this._notificationService = notificationService;
            _esewaService = esewaService;
            this._configuration = _configuration;
            _logger = logger;
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
                .Where(p => p.SellerId == sellerId && !p.IsDeleted)
                .Include(p => p.PropertyImages)
                .OrderByDescending(p => p.Status == "Approved") // ✅ Approved ones first
                .ThenByDescending(p => p.CreatedAt) // Optional: then order by newest
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
                property.IsDeleted = true;
                _context.properties.Update(property);
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
                    .Where(p => p.SellerId == sellerId && p.IsDeleted == false)
                    .ToListAsync();

                var totalListings = properties.Count;
                var pendingApprovals = properties.Count(p => p.Status == "Pending" && p.IsDeleted == false);
                var activeProperties = properties.Count(p => p.Status == "Approved" && p.IsDeleted == false);
                var newInquiries = properties.Count(p => p.Status == "Rejected" && p.IsDeleted == false);
                var soldProperties = properties.Count(p => p.Status == "Sold" && p.IsDeleted == false);

                // Get sold properties grouped by month for the chart
                var soldPropertiesByMonth = properties
                    .Where(p => p.Status == "Sold" && p.SoldDate != null && p.IsDeleted == false)
                    .GroupBy(p => new { Month = p.SoldDate.Value.Month, Year = p.SoldDate.Value.Year })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .Take(7)
                    .ToList();
                // In your Dashboard action method, you already have:
                var propertyTypeDistribution = properties
                    .Where(p => p.Status == "Approved" && p.IsDeleted == false)
                    .GroupBy(p => p.PropertyType)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Type)
                    .ToList();

                // And you're setting these ViewBag properties:
                ViewBag.PropertyTypeLabels = propertyTypeDistribution.Select(x => x.Type).ToList();
                ViewBag.PropertyTypeCounts = propertyTypeDistribution.Select(x => x.Count).ToList();

                ViewBag.TotalListings = totalListings;
                ViewBag.PendingApprovals = pendingApprovals;
                ViewBag.ActiveProperties = activeProperties;
                ViewBag.NewInquiries = newInquiries;
                ViewBag.SoldProperties = soldProperties;
                ViewBag.Name = firstName;

                // Pass the sold properties data to the view
                ViewBag.SoldPropertiesMonths = soldPropertiesByMonth.Select(x => x.Date.ToString("MMM")).ToList();
                ViewBag.SoldPropertiesCounts = soldPropertiesByMonth.Select(x => x.Count).ToList();

                return View(properties);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
        [HttpGet]
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

            // Instead of saving to database, pass to confirmation view
            var confirmationModel = new BoostViewModel
            {
                PropertyId = propertyId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                SelectedHours = model.SelectedHours,
                TotalPrice = model.TotalPrice,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(model.SelectedHours)
            };

            // Pass to confirmation view instead of saving
            return View("ConfirmBoost", confirmationModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBoost(BoostViewModel model)
        {
            // Add this before the ModelState.IsValid check
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in {state.Key}: {state.Value.Errors[0].ErrorMessage}");
                    // Or use a breakpoint here to inspect in the debugger
                }
            }

            if (!ModelState.IsValid) return View(model);

            var boostedProperty = new BoostedProperty
            {
                PropertyId = model.PropertyId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Hours = model.SelectedHours,
                Price = model.TotalPrice,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(model.SelectedHours),
                PaymentStatus = "Pending"
            };

            try
            {
                _context.BoostedProperties.Add(boostedProperty);
                await _context.SaveChangesAsync();

                // Log success
                Console.WriteLine($"Successfully saved boost ID: {boostedProperty.Id}");

                return RedirectToAction("InitiateEsewaPayment", new { boostId = boostedProperty.Id });
            }
            catch (Exception ex)
            {
                // Log the full error details
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner Exception: " + ex.InnerException.Message;
                }

                Console.WriteLine($"Error saving boost: {errorMessage}");
                ModelState.AddModelError("", "Failed to save boost: " + errorMessage);
                return View(model);
            }

            return Redirect(Url.Action("InitiateEsewaPayment", "Seller", new { boostId = boostedProperty.Id }));
        }
        public async Task<IActionResult> InitiateEsewaPayment(int boostId)
        {
            try
            {
                // Get the boost record
                var boost = await _context.BoostedProperties
                    .FirstOrDefaultAsync(b => b.Id == boostId && b.PaymentStatus == "Pending");

                if (boost == null)
                {
                    return RedirectToAction("BoostFailed", new { error = "invalid_boost" });
                }

                // Configuration
                string secretKey = "8gBm/:&EnhH.1/q";
                string merchantCode = "EPAYTEST";
                string paymentUrl = "https://rc-epay.esewa.com.np/api/epay/main/v2/form";

                // Payment parameters
                string totalAmount = boost.Price.ToString("0.00", CultureInfo.InvariantCulture);
                string transactionUuid = Guid.NewGuid().ToString();

                // Store the transaction UUID in the boost record
                boost.TransactionUuid = transactionUuid;
                await _context.SaveChangesAsync();

                // Generate signature
                string signatureString = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={merchantCode}";
                string signature = GenerateEsewaSignature(signatureString, secretKey);

                // Prepare view model
                var viewModel = new EsewaPaymentViewModel
                {
                    Amount = totalAmount,
                    TotalAmount = totalAmount,
                    TransactionUuid = transactionUuid,
                    ProductCode = merchantCode,
                    ProductServiceCharge = "0.00",
                    Signature = signature,
                    SuccessUrl = Url.Action("EsewaSuccess", "Seller", null, Request.Scheme),
                    FailureUrl = Url.Action("EsewaFailure", "Seller", null, Request.Scheme),
                    SignedFieldNames = "total_amount,transaction_uuid,product_code",
                    PaymentUrl = paymentUrl
                };

                return View("EsewaPaymentForm", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating eSewa payment");
                return RedirectToAction("BoostFailed", new { error = "payment_initiation_failed" });
            }
        }

        // Generate Base64 signature
        private string GenerateEsewaSignature(string signatureString, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                return Convert.ToBase64String(hashBytes);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EsewaSuccess([FromQuery] string data)
        {
            try
            {
                // Decode the base64 encoded data
                var decodedData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data));

                // Log the decoded data for debugging
                _logger.LogInformation($"Decoded eSewa response: {decodedData}");

                // Parse the JSON data
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var paymentResponse = JsonSerializer.Deserialize<EsewaPaymentResponse>(decodedData, options);

                if (paymentResponse == null || paymentResponse.Status != "COMPLETE")
                {
                    _logger.LogWarning($"Payment incomplete: {decodedData}");
                    return RedirectToAction("BoostFailed", new { error = "payment_incomplete" });
                }

                // Log the transaction UUID we're looking for
                _logger.LogInformation($"Looking for boost with transaction UUID: {paymentResponse.TransactionUuid}");

                // Try to find by transaction UUID first
                var boostedProperty = await _context.BoostedProperties
                    .FirstOrDefaultAsync(b => b.TransactionUuid == paymentResponse.TransactionUuid && b.PaymentStatus == "Pending");

                // If not found by UUID, fall back to finding the most recent pending boost
                if (boostedProperty == null)
                {
                    _logger.LogWarning("Boost not found by transaction UUID, falling back to most recent pending boost");
                    boostedProperty = await _context.BoostedProperties
                        .Where(b => b.PaymentStatus == "Pending")
                        .OrderByDescending(b => b.Id)
                        .FirstOrDefaultAsync();
                }

                if (boostedProperty == null)
                {
                    _logger.LogError("No pending boosted property found");
                    return RedirectToAction("BoostFailed", new { error = "boost_not_found" });
                }

                // Log which boost we found
                _logger.LogInformation($"Found boost ID: {boostedProperty.Id} for property ID: {boostedProperty.PropertyId}");

                // Update payment status
                boostedProperty.PaymentStatus = "Paid";
                boostedProperty.TransactionId = paymentResponse.TransactionCode;
                boostedProperty.IsActive = true;
                boostedProperty.StartTime = DateTime.UtcNow;
                boostedProperty.EndTime = DateTime.UtcNow.AddHours(boostedProperty.Hours);

                await _context.SaveChangesAsync();

                // Redirect to success page
                return RedirectToAction("BoostSuccess", new { id = boostedProperty.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing eSewa payment: " + ex.Message);
                return RedirectToAction("BoostFailed", new { error = "processing_error" });
            }
        }

        public IActionResult EsewaFailure()
        {
            return View("BoostFailed");
        }

        // Response model
        public class EsewaPaymentResponse
        {
            [JsonPropertyName("transaction_code")]
            public string TransactionCode { get; set; } = string.Empty;

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("total_amount")]
            public string TotalAmount { get; set; } = string.Empty;

            [JsonPropertyName("transaction_uuid")]
            public string TransactionUuid { get; set; } = string.Empty;

            [JsonPropertyName("product_code")]
            public string ProductCode { get; set; } = string.Empty;

            [JsonPropertyName("signed_field_names")]
            public string SignedFieldNames { get; set; } = string.Empty;

            [JsonPropertyName("signature")]
            public string Signature { get; set; } = string.Empty;
        }

        public IActionResult BoostSuccess(int id)
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
        public IActionResult BoostFailed(string errorMessage)
        {
            ViewBag.ErrorMessage = errorMessage;
            return View();
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
                .Where(p => p.SellerId == userId && p.Status!= "Sold" && p.IsDeleted==false)
                .OrderByDescending(p => p.SoldDate)
                .ToListAsync();

            // Get boosted properties for this seller
            var boostedProperties = await _context.BoostedProperties
                .Include(bp => bp.Property)
                .Where(bp => bp.Property.SellerId == userId)
                .ToListAsync();

            ViewBag.BoostedProperties = boostedProperties;

            return View(properties);
        }

        // Add these methods to your SellerController class

        [HttpGet]
        public async Task<IActionResult> ViewingRequests()
        {
            // Get the current seller's ID
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get all properties belonging to this seller
            var sellerProperties = await _context.properties
                .Where(p => p.SellerId == sellerId)
                .Select(p => p.Id)
                .ToListAsync();

            // Get all viewing requests for these properties
            var viewingRequests = await _context.PropertyViewingRequests
                .Include(r => r.Properties)
                .Include(r => r.Buyer)
                .Where(r => sellerProperties.Contains(r.PropertyId) && (r.Status == "Pending" || r.Status == "Approved"))
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(viewingRequests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.PropertyViewingRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            // Verify the property belongs to the current seller
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId);

            if (property == null)
            {
                return Forbid();
            }

            // Update request status
            request.Status = "Approved";
            await _context.SaveChangesAsync();

            // Notify the buyer
            if (request.BuyerId > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    "Viewing Request Approved",
                    $"Your request to view property '{property.Title}' has been approved.",
                    "ViewingApproved",
                    request.BuyerId,
                    property.Id
                );
            }

            return RedirectToAction(nameof(ViewingRequests));
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await _context.PropertyViewingRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            // Verify the property belongs to the current seller
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId);

            if (property == null)
            {
                return Forbid();
            }

            // Update request status
            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            // Notify the buyer
            if (request.BuyerId > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    "Viewing Request Rejected",
                    $"Your request to view property '{property.Title}' has been rejected.",
                    "ViewingRejected",
                    request.BuyerId,
                    property.Id
                );
            }

            return RedirectToAction(nameof(ViewingRequests));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            // Get the viewing request
            var request = await _context.PropertyViewingRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            // Verify the property belongs to the current seller
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId);

            if (property == null)
            {
                return Forbid();
            }

            // Update property status to "Sold"
            property.Status = "Sold";
            property.SoldDate = DateTime.UtcNow.AddMinutes(345);

            // Update request status to "Completed"
            request.Status = "Completed";

            // Update any other pending requests for this property to "Cancelled" since the property is now sold
            var otherRequests = await _context.PropertyViewingRequests
                .Where(r => r.PropertyId == property.Id && r.Id != request.Id && r.Status == "Pending")
                .ToListAsync();

            foreach (var otherRequest in otherRequests)
            {
                otherRequest.Status = "Cancelled";

                // Notify the buyer that the property is no longer available
                if (otherRequest.BuyerId > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        "Property No Longer Available",
                        $"The property '{property.Title}' you requested to view has been sold.",
                        "PropertySold",
                        otherRequest.BuyerId,
                        property.Id
                    );
                }
            }

            await _context.SaveChangesAsync();

            // Notify the buyer who viewed the property
            if (request.BuyerId > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    "Property Sold",
                    $"The property '{property.Title}' you viewed has been sold. You can now rate the seller from your purchases page.",
                    "PropertySold",
                    request.BuyerId,
                    property.Id
                );
            }

            TempData["SuccessMessage"] = $"Property '{property.Title}' has been marked as sold.";
            return RedirectToAction(nameof(ViewingRequests));
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsNotSold(int id)
        {
            // Get the viewing request
            var request = await _context.PropertyViewingRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            // Verify the property belongs to the current seller
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.SellerId == sellerId);

            if (property == null)
            {
                return Forbid();
            }

            // Update property status back to "Approved"
            property.Status = "Approved";

            // Update request status to "Viewed" - this will ensure only the View button shows
            request.Status = "Viewed";

            await _context.SaveChangesAsync();

            // Notify the buyer who viewed the property
            if (request.BuyerId > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    "Property Status Updated",
                    $"The property '{property.Title}' is now available again.",
                    "PropertyAvailable",
                    request.BuyerId,
                    property.Id
                );
            }

            TempData["SuccessMessage"] = $"Property '{property.Title}' has been marked as not sold.";
            return RedirectToAction(nameof(ViewingRequests));
        }
        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            var request = await _context.PropertyViewingRequests
                .Include(r => r.Properties)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Verify the property belongs to the current seller
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (request.Properties.SellerId != sellerId)
            {
                return Forbid();
            }

            return Json(new
            {
                requestId = request.Id,
                propertyId = request.PropertyId,
                propertyTitle = request.Properties.Title,
                buyerName = request.BuyerName,
                buyerEmail = request.BuyerEmail,
                buyerContact = request.BuyerContact,
                viewingDate = request.ViewingDate,
                viewingTime = request.ViewingTime,
                notes = request.Notes,
                status = request.Status,
                requestedAt = request.RequestedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> MyRatings()
        {
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var ratings = await _context.SellerRatings
                .Include(r => r.Buyer)
                .Include(r => r.Property)
                .Where(r => r.SellerId == sellerId)
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
        // Add these methods to your SellerController class

        [HttpGet]
        public async Task<IActionResult> SoldProperties()
        {
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get all properties belonging to this seller that are marked as sold
            var soldProperties = await _context.properties
                .Include(p => p.PropertyImages)
                .Where(p => p.SellerId == sellerId && p.Status == "Sold" && p.IsDeleted==false)
                .ToListAsync();

            // Get all completed viewing requests for these properties
            var propertyIds = soldProperties.Select(p => p.Id).ToList();
            var completedRequests = await _context.PropertyViewingRequests
                .Include(r => r.Properties)
                .Include(r => r.Buyer)
                .Where(r => propertyIds.Contains(r.PropertyId) && r.Status == "Completed")
                .ToListAsync();

            // Check which buyers have already been rated by this seller
            var ratedBuyerProperties = await _context.BuyerRatings
                .Where(r => r.SellerId == sellerId)
                .Select(r => new { r.BuyerId, r.PropertyId })
                .ToListAsync();

            // Create view model
            var viewModel = completedRequests.Select(r => new SoldPropertyViewModel
            {
                ViewingRequestId = r.Id,
                PropertyId = r.PropertyId,
                PropertyTitle = r.Properties.Title,
                PropertyDescription = r.Properties.Description,
                PropertyType = r.Properties.PropertyType,
                PropertyPrice = r.Properties.Price,
                PropertyLocation = $"{r.Properties.District}, {r.Properties.City}, {r.Properties.Province}",
                PropertyImage = r.Properties.PropertyImages.FirstOrDefault()?.Photo != null
                    ? "/Images/" + r.Properties.PropertyImages.FirstOrDefault()?.Photo
                    : "/placeholder.svg?height=200&width=300",
                BuyerId = r.BuyerId,
                BuyerName = r.Buyer?.FullName ?? r.BuyerName,
                CompletionDate = r.RequestedAt,
                HasBeenRated = ratedBuyerProperties.Any(rp => rp.BuyerId == r.BuyerId && rp.PropertyId == r.PropertyId)
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> RateBuyer(int propertyId, int? requestId = null)
        {
            // Get the property
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null)
            {
                return NotFound();
            }

            // Check if the property belongs to the current seller
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (property.SellerId != sellerId)
            {
                return Forbid();
            }

            // Check if the property is sold
            if (property.Status != "Sold")
            {
                TempData["ErrorMessage"] = "You can only rate buyers for properties that have been sold.";
                return RedirectToAction("SoldProperties");
            }

            // Get the viewing request
            PropertyViewingRequest request = null;
            if (requestId.HasValue)
            {
                request = await _context.PropertyViewingRequests
                    .Include(r => r.Buyer)
                    .FirstOrDefaultAsync(r => r.Id == requestId.Value);
            }
            else
            {
                // If no specific request ID is provided, get the completed request for this property
                request = await _context.PropertyViewingRequests
                    .Include(r => r.Buyer)
                    .FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.Status == "Completed");
            }

            if (request == null)
            {
                TempData["ErrorMessage"] = "No completed viewing request found for this property.";
                return RedirectToAction("SoldProperties");
            }

            // Check if the buyer has already been rated for this property
            var existingRating = await _context.BuyerRatings
                .FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.BuyerId == request.BuyerId && r.SellerId == sellerId);

            if (existingRating != null)
            {
                TempData["ErrorMessage"] = "You have already rated this buyer for this property.";
                return RedirectToAction("SoldProperties");
            }

            // Create the view model
            var viewModel = new BuyerRatingViewModel
            {
                PropertyId = propertyId,
                PropertyTitle = property.Title,
                BuyerId = request.BuyerId,
                BuyerName = request.Buyer?.FullName ?? request.BuyerName,
                ViewingRequestId = request.Id
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateBuyer(BuyerRatingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the seller ID
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Create the rating
            var rating = new BuyerRating
            {
                SellerId = sellerId,
                BuyerId = model.BuyerId,
                PropertyId = model.PropertyId,
                Rating = model.Rating,
                Review = model.Review,
                ViewingRequestId = model.ViewingRequestId,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            _context.BuyerRatings.Add(rating);
            await _context.SaveChangesAsync();

            // Notify the buyer
            await _notificationService.CreateNotificationAsync(
                "New Rating Received",
                $"You received a {model.Rating}-star rating for property '{model.PropertyTitle}'.",
                "NewRating",
                model.BuyerId,
                model.PropertyId
            );

            TempData["SuccessMessage"] = "Thank you for your rating and review!";
            return RedirectToAction("SoldProperties");
        }

       
    }
    
}
