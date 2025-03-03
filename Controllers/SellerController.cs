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
            string filename = "";
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get User ID from claims

            // Handle photo upload
           

            // Save base property details
            var propertyModel = new Properties
            {
                PropertyType = addUserRequest.PropertyType,
                Title = addUserRequest.Title,
                Price = addUserRequest.Price,
                Description = addUserRequest.Description,
                District = addUserRequest.District,
                City = addUserRequest.City,
                Province = addUserRequest.Province,
                Status = "Pending",
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
                        if (photo.Length > 0) // Use .Length on individual IFormFile
                        {
                            string photoFilename = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName); // Renamed filename to photoFilename
                            string filePath = Path.Combine(uploadFolder, photoFilename);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await photo.CopyToAsync(fileStream);
                            }

                            propertyImages.Add(new PropertyImage
                            {
                                PropertyId = propertyModel.Id, // Foreign key
                                Photo = photoFilename // Use the new photoFilename variable
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
            
            // Save specific property type details
            if (addUserRequest.PropertyType == "House")
            {
                var house = new House
                {
                    Bedrooms = addUserRequest.Bedrooms,
                    Kitchens = addUserRequest.Kitchens,
                    SittingRooms = addUserRequest.SittingRooms,
                    Bathrooms = addUserRequest.Bathrooms,
                    Floors = addUserRequest.Floors,
                    Area = addUserRequest.Area,
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
                    PropertyID = propertyModel.Id // Foreign key
                };
                await _context.Apartments.AddAsync(apartment);
            }
            else if (addUserRequest.PropertyType == "Land")
            {
                var land = new Land
                {
                    Area = addUserRequest.Area,
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
            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Convert to int
            var properties = await _context.properties
                .Where(p => p.SellerId == sellerId) // Filter by logged-in seller
                .Include(p => p.PropertyImages) // Include related images
                .ToListAsync();

            if (properties == null || !properties.Any())
            {
                properties = new List<Properties>(); // Avoid null issues
            }

            return View(properties); // Pass data to the view
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
                };
                return View(editModel);
            }
            return RedirectToAction("Listings");
        }

        //[HttpPost]
        //public async Task<IActionResult> EditProperty(AddProperties model, IFormFile photo)
        //{
        //    var property = await _context.properties.FindAsync(model.PropertyId);
        //    if (property != null)
        //    {
        //        property.Title = model.Title;
        //        property.Description = model.Description;
        //        property.Price = model.Price;
        //        property.City = model.City;
        //        property.District = model.District;

        //        if (photo != null && photo.Length > 0)
        //        {
        //            try
        //            {
        //                string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");
        //                string filename = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
        //                string filePath = Path.Combine(uploadFolder, filename);
                       
        //                using (var fileStream = new FileStream(filePath, FileMode.Create))
        //                {
        //                    await photo.CopyToAsync(fileStream);
        //                }

        //                // Delete old image if it exists
        //                if (!string.IsNullOrEmpty(property.Photo))
        //                {
        //                    var oldImagePath = Path.Combine(uploadFolder, property.Photo);
        //                    if (System.IO.File.Exists(oldImagePath))
        //                    {
        //                        System.IO.File.Delete(oldImagePath);
        //                    }
        //                }

        //                property.Photo = filename;
        //            }
        //            catch (Exception ex)
        //            {
        //                ModelState.AddModelError("", $"File upload failed: {ex.Message}");
        //                return View(model);
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Listings");
        //    }
        //    return RedirectToAction("Listings");
        //}
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
        // Dashboard page
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Get current seller's ID
                var fullName = User.FindFirstValue("FullName"); // Retrieve the full name
                var firstName = fullName.Split(' ').FirstOrDefault(); // Get the first part of the name (the first name)
                                                                          // Now you can use 'firstName' as the username or display name
                


                // Fetch seller's properties
                var properties = await _context.properties
                    .Where(p => p.SellerId == sellerId)
                    .ToListAsync();

                // Calculate statistics
                var totalListings = properties.Count;
                var pendingApprovals = properties.Count(p => p.Status == "Pending");
                var activeProperties = properties.Count(p => p.Status == "Approved");
                var newInquiries = properties.Count(p => p.Status == "Rejected"); // Placeholder, replace with actual logic

                // Pass data to the view
                ViewBag.TotalListings = totalListings;
                ViewBag.PendingApprovals = pendingApprovals;
                ViewBag.ActiveProperties = activeProperties;
                ViewBag.NewInquiries = newInquiries;
                ViewBag.Name = firstName;

                return View(properties);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}"); // Temporary error display
            }
        }
}
}
