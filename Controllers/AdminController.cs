using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Propertease.Models;
using Propertease.Repos;
using System.Linq;
using System.Threading.Tasks;

namespace Propertease.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ProperteaseDbContext _context;
        private readonly PropertyRepository _propertyService;
        private readonly EmailService _emailService;

        public AdminController(ProperteaseDbContext context, PropertyRepository propertyService, EmailService emailService)
        {
            _context = context;
            _propertyService = propertyService;
            _emailService = emailService;
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

            // Pass the data to the view
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveProperties = activeProperties;
            ViewBag.PendingApprovals = pendingApprovals;

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
