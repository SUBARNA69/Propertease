using Microsoft.AspNetCore.Mvc;
using Propertease.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Propertease.Controllers
{
    public class AdminController : Controller
    {
        private readonly ProperteaseDbContext _context;
        private readonly PropertyRepository _propertyService;

        public AdminController(ProperteaseDbContext context, PropertyRepository propertyService)
        {
            _context = context;
            _propertyService = propertyService;
        }

        public IActionResult Dashboard()
        {
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
            }
            return RedirectToAction("AdminRequests");
        }

        // Action to view approved properties
        public IActionResult ApprovedProperties()
        {
            var approvedProperties = _context.properties
                                             .Where(p => p.Status == "Approved")
                                             .ToList();

            return View(approvedProperties);  // Pass approved properties to the view
        }
        public IActionResult AllProperties()
        {
            var allProperties = _context.properties.ToList();
            return View(allProperties);
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
    }
}
