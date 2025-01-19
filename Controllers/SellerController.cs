using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Propertease.Models;
using System.Security.Claims;

namespace Propertease.Controllers
{
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
            if (addUserRequest.photo != null && addUserRequest.photo.Length > 0)
            {
                try
                {
                    string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");
                    filename = Guid.NewGuid().ToString() + "_" + Path.GetFileName(addUserRequest.photo.FileName);
                    string filePath = Path.Combine(uploadFolder, filename);

                    // Save photo to the server
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await addUserRequest.photo.CopyToAsync(fileStream);
                    }
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
                Photo = filename,
                Status = "Pending",
                SellerId = int.Parse(sellerId), // Automatically assign SellerId
            };

            await _context.properties.AddAsync(propertyModel);
            await _context.SaveChangesAsync(); // Save and get PropertyID

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
            var properties = await _context.properties.ToListAsync(); // Fetch data from database
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

        [HttpPost]
        public async Task<IActionResult> EditProperty(AddProperties model, IFormFile photo)
        {
            var property = await _context.properties.FindAsync(model.PropertyId);
            if (property != null)
            {
                property.Title = model.Title;
                property.Description = model.Description;
                property.Price = model.Price;
                property.City = model.City;
                property.District = model.District;

                if (photo != null && photo.Length > 0)
                {
                    try
                    {
                        string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");
                        string filename = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
                        string filePath = Path.Combine(uploadFolder, filename);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(fileStream);
                        }

                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(property.Photo))
                        {
                            var oldImagePath = Path.Combine(uploadFolder, property.Photo);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        property.Photo = filename;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"File upload failed: {ex.Message}");
                        return View(model);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Listings");
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
        // Dashboard page
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
