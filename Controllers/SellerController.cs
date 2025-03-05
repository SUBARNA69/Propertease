using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Propertease.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace Propertease.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        IWebHostEnvironment webHostEnvironment;
        private readonly ProperteaseDbContext _context;

        // Inject the ApplicationDbContext into the controller
        public SellerController(ProperteaseDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            this.webHostEnvironment = webHostEnvironment;
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

            TempData["Notification"] = $"Your property '{propertyModel.Title}' has been added and is awaiting approval by an admin.";

            // Redirect to Listings page
            return RedirectToAction("Listings");
        }

        public IActionResult ViewPropertyDetails(int id)
        {
            var property = _context.properties.FirstOrDefault(p => p.Id == id);
            if (property == null)
            {
                return NotFound();
            }

            return View(property);
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
            var property = await _context.properties.FirstOrDefaultAsync(x => x.Id == id);
            if (property != null)
            {
                var editModel = new AddProperties
                {
                    PropertyId = property.Id,
                    Title = property.Title,
                    Description = property.Description,
                    Price = property.Price,
                    City = property.City,
                    District = property.District,
                    // You could add additional fields here if needed
                };
                return View(editModel);
            }
            return RedirectToAction("Listings");
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
    }
}
