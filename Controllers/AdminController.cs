using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Propertease.Models;
using Propertease.Repos;
using System.Threading.Tasks;
using Propertease.Services;
namespace Propertease.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly ProperteaseDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly PropertyRepository _propertyService;
        private readonly EmailService _emailService;

        public AdminController(ProperteaseDbContext context, PropertyRepository propertyService, EmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _propertyService = propertyService;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            // Calculate total number of users who are buyers or sellers
            var totalUsers = _context.Users
                .Count(u => u.Role == "Buyer" || u.Role == "Seller");

            // Calculate total number of active properties
            var activeProperties = _context.properties
                .Count(p => p.Status == "Approved");

            // Calculate total number of pending approvals
            var pendingApprovals = _context.properties
                .Count(p => p.Status == "Pending");

            // Get the last 6 months
            var today = DateTime.Today;
            var lastSixMonths = Enumerable.Range(0, 6)
                .Select(i => today.AddMonths(-i))
                .Select(date => new {
                    Month = date.ToString("MMM"),
                    Year = date.Year,
                    MonthNumber = date.Month
                })
                .OrderBy(d => d.Year)
                .ThenBy(d => d.MonthNumber)
                .ToList();

            // Monthly User Registrations (Bar Chart 1)
            var monthlyUserRegistrations = new List<object>();
            foreach (var month in lastSixMonths)
            {
                var startDate = new DateTime(month.Year, month.MonthNumber, 1);
                var endDate = startDate.AddMonths(1);

                var buyerCount = _context.Users
                    .Count(u => u.Role == "Buyer" &&
                           u.CreatedAt >= startDate &&
                           u.CreatedAt < endDate);

                var sellerCount = _context.Users
                    .Count(u => u.Role == "Seller" &&
                           u.CreatedAt >= startDate &&
                           u.CreatedAt < endDate);

                monthlyUserRegistrations.Add(new
                {
                    month = month.Month,
                    buyers = buyerCount,
                    sellers = sellerCount
                });
            }

            // Monthly Property Listings (Bar Chart 2)
            var monthlyPropertyListings = new List<object>();
            foreach (var month in lastSixMonths)
            {
                var startDate = new DateTime(month.Year, month.MonthNumber, 1);
                var endDate = startDate.AddMonths(1);

                var approvedCount = _context.properties
                    .Count(p => p.Status == "Approved" &&
                           p.CreatedAt >= startDate &&
                           p.CreatedAt < endDate);

                var pendingCount = _context.properties
                    .Count(p => p.Status == "Pending" &&
                           p.CreatedAt >= startDate &&
                           p.CreatedAt < endDate);

                monthlyPropertyListings.Add(new
                {
                    month = month.Month,
                    approved = approvedCount,
                    pending = pendingCount
                });
            }

            // Property Types Distribution (Bar Chart 3)
            var propertyTypesData = _context.properties
                .Where(p => p.Status == "Approved")
                .GroupBy(p => p.PropertyType)
                .Select(g => new { type = g.Key, count = g.Count() })
                .ToList();

            // Property Growth Line Chart Data
            var propertyGrowthData = new List<object>();
            foreach (var month in lastSixMonths)
            {
                var endDate = new DateTime(month.Year, month.MonthNumber, 1).AddMonths(1);

                var totalProperties = _context.properties
                    .Count(p => p.CreatedAt < endDate);

                propertyGrowthData.Add(new
                {
                    month = month.Month,
                    total = totalProperties
                });
            }
            // Get recent approved properties for the activity feed
            var recentApprovedProperties = _context.properties
                .Where(p => p.Status == "Approved")
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new {
                    Id = p.Id,
                    Title = p.Title,
                    Location = p.City,
                    ApprovedDate = p.CreatedAt,
                    Price = p.Price
                })
                .ToList();
            // Pass the data to the view
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveProperties = activeProperties;
            ViewBag.PendingApprovals = pendingApprovals;
            ViewBag.MonthlyUserRegistrations = Newtonsoft.Json.JsonConvert.SerializeObject(monthlyUserRegistrations);
            ViewBag.MonthlyPropertyListings = Newtonsoft.Json.JsonConvert.SerializeObject(monthlyPropertyListings);
            ViewBag.PropertyTypesData = Newtonsoft.Json.JsonConvert.SerializeObject(propertyTypesData);
            ViewBag.PropertyGrowthData = Newtonsoft.Json.JsonConvert.SerializeObject(propertyGrowthData);
            ViewBag.RecentApprovedProperties = recentApprovedProperties;

            return View();
        }

        // Action for displaying requests (pending properties)
        public IActionResult AdminRequests()
        {
            var pendingProperties = _context.properties
                .Where(p => p.Status == "Pending")
                .ToList();

            return View(pendingProperties);
        }

        // Action to view property details
        public async Task<IActionResult> ViewPropertyDetails(int id)
        {
            // Use the repository to fetch detailed property information
            var propertyDetails = await _propertyService.GetPropertyDetails(id);

            if (propertyDetails == null)
            {
                return NotFound();
            }

            return View(propertyDetails);
        }

        // Action to approve a property
        [HttpPost]
        public async Task<IActionResult> ApproveProperty(int id)
        {
            var property = await _context.properties.FindAsync(id);
            if (property != null)
            {
                property.Status = "Approved";
                await _context.SaveChangesAsync();

                // Fetch seller's email (assuming it's stored in the Users table)
                var seller = await _context.Users.FindAsync(property.SellerId);
                if (seller != null)
                {
                    var subject = "Your Property Has Been Approved";
                    var body = $"Dear {seller.FullName},<br><br>Your property '{property.Title}' has been approved.<br><br>Thank you!";
                    await _emailService.SendEmailAsync(seller.Email, subject, body);
                }
                await _notificationService.CreateNotificationAsync(
                   "Property Approved",
                   $"Your property '{property.Title}' has been approved and is now active.",
                   "PropertyApproved",
                   property.SellerId
                   
   );
            }
            return RedirectToAction("AdminRequests");
        }

        // Action to reject a property
        [HttpPost]
        public async Task<IActionResult> RejectProperty(int id)
        {
            var property = await _context.properties.FindAsync(id);
            if (property != null)
            {
                property.Status = "Rejected";
                await _context.SaveChangesAsync();

                // Fetch seller's email (assuming it's stored in the Users table)
                var seller = await _context.Users.FindAsync(property.SellerId);
                if (seller != null)
                {
                    var subject = "Your Property Has Been Rejected";
                    var body = $"Dear {seller.FullName},<br><br>Your property '{property.Title}' has been rejected.<br><br>Thank you!";
                    await _emailService.SendEmailAsync(seller.Email, subject, body);
                }
                await _notificationService.CreateNotificationAsync(
                   "Property Rejected",
                   $"Your property '{property.Title}' has been rejected.",
                   "PropertyRejected",
                   property.SellerId
               );
            }
            return RedirectToAction("AdminRequests");
        }

        // Action to view approved properties
        public IActionResult ApprovedProperties()
        {
            var approvedProperties = _context.properties
                                             .Where(p => p.Status == "Approved")
                                             .ToList();

            return View(approvedProperties);
        }

        public IActionResult AllProperties()
        {
            var properties = _context.properties
                .Include(p => p.PropertyImages)
                .ToList();

            return View(properties);
        }

        // Action to delete a property
        [HttpPost]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var property = await _context.properties.FindAsync(id);
            if (property != null)
            {
                _context.properties.Remove(property);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AllProperties");
        }

        // Action to display the user list in the UsersManagement view
        public IActionResult UsersManagement()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // Action to view user details
        public async Task<IActionResult> ViewUserDetails(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // Action to delete a user
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UsersManagement");
        }
    }
}
