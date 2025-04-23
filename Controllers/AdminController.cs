using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Propertease.Models;
using Propertease.Repos;
using System.Threading.Tasks;
using Propertease.Services;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Drawing;
using ClosedXML.Excel;
namespace Propertease.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly ProperteaseDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly PropertyRepository _propertyService;
        private readonly EmailService _emailService;
        private readonly ILogger<AdminController> _logger;


        public AdminController(ILogger<AdminController> logger, ProperteaseDbContext context, PropertyRepository propertyService, EmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _propertyService = propertyService;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
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
            decimal totalEarnings = _context.BoostedProperties
        .Where(bp => bp.PaymentStatus == "Paid")
        .Sum(bp => bp.Price);

            // Get monthly earnings data for the chart
            var monthlyEarnings = new List<object>();
            foreach (var month in lastSixMonths)
            {
                var startDate = new DateTime(month.Year, month.MonthNumber, 1);
                var endDate = startDate.AddMonths(1);

                var monthlyAmount = _context.BoostedProperties
                    .Where(bp => bp.PaymentStatus == "Paid" &&
                           bp.StartTime >= startDate &&
                           bp.StartTime < endDate)
                    .Sum(bp => bp.Price);

                monthlyEarnings.Add(new
                {
                    month = month.Month,
                    earnings = monthlyAmount
                });
            }
            // Pass the data to the view
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveProperties = activeProperties;
            ViewBag.PendingApprovals = pendingApprovals;
            ViewBag.MonthlyUserRegistrations = Newtonsoft.Json.JsonConvert.SerializeObject(monthlyUserRegistrations);
            ViewBag.MonthlyPropertyListings = Newtonsoft.Json.JsonConvert.SerializeObject(monthlyPropertyListings);
            ViewBag.PropertyTypesData = Newtonsoft.Json.JsonConvert.SerializeObject(propertyTypesData);
            ViewBag.PropertyGrowthData = Newtonsoft.Json.JsonConvert.SerializeObject(propertyGrowthData);
            ViewBag.RecentApprovedProperties = recentApprovedProperties;
            ViewBag.TotalEarnings = totalEarnings;
            ViewBag.MonthlyEarnings = Newtonsoft.Json.JsonConvert.SerializeObject(monthlyEarnings);


            return View();
        }

        // Action for displaying requests (pending properties)
        public IActionResult AdminRequests()
        {
            var pendingProperties = _context.properties
                .Where(p => p.Status == "Pending")
                .Include(p => p.PropertyImages) // Include the related images
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
                    var body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 5px; }}
        .header {{ background-color: #4a6fdc; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; background: white; border: 1px solid #ddd; border-top: none; border-radius: 0 0 5px 5px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
        .button-container {{ text-align: center; margin-top: 20px; }}
        .button {{ display: inline-block; background-color: #4a6fdc; color: white; text-decoration: none; 
                   padding: 12px 20px; border-radius: 5px; font-weight: bold; font-size: 16px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Property Approval Notification</h2>
        </div>
        <div class='content'>
            <p>Dear {seller.FullName},</p>
            <p>We are pleased to inform you that your property listing <strong>'{property.Title}'</strong> has been successfully approved and is now live on <strong>ProperTease</strong>.</p>
            <p>Potential buyers can now view your listing and get in touch with you. Make sure to check your dashboard for any inquiries and manage your listing as needed.</p>
            <p>We wish you the best in finding the right buyer for your property!</p>
            <p>Thank you,<br><strong>The ProperTease Team</strong></p>
        </div>
        <div class='footer'>
            <p>If you have any questions or need assistance, feel free to contact our support team.</p>
        </div>
    </div>
</body>
</html>";
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
                    var body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 5px; }}
        .header {{ background-color: #dc4a4a; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; background: white; border: 1px solid #ddd; border-top: none; border-radius: 0 0 5px 5px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
        .button-container {{ text-align: center; margin-top: 20px; }}
        .button {{ display: inline-block; background-color: #4a6fdc; color: white; text-decoration: none; 
                   padding: 12px 20px; border-radius: 5px; font-weight: bold; font-size: 16px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Property Listing Rejected</h2>
        </div>
        <div class='content'>
            <p>Dear {seller.FullName},</p>
            <p>We regret to inform you that your property listing <strong>'{property.Title}'</strong> has been rejected: </p>
            <p>If you believe this was an error or need assistance in modifying your listing, please review our guidelines and make the necessary adjustments.</p>
            <div class='button-container'>
            </div>
            <p>We encourage you to update your listing and resubmit it for approval.</p>
            <p>Thank you for using <strong>ProperTease</strong>. We appreciate your efforts in maintaining high-quality listings on our platform.</p>
            <p>Best regards,<br><strong>The ProperTease Team</strong></p>
        </div>
        <div class='footer'>
            <p>If you have any questions, feel free to contact our support team.</p>
        </div>
    </div>
</body>
</html>";
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
                                             .Where(p => p.Status == "Approved" && p.IsDeleted == false)
                                             .ToList();

            return View(approvedProperties);
        }

        public IActionResult AllProperties()
        {
            var properties = _context.properties
                .Include(p => p.PropertyImages)
                .Where(p => p.IsDeleted == false && p.IsDeleted == false)
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
                property.IsDeleted = true;
                _context.properties.Update(property);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AllProperties");
        }

        // Action to display the user list in the UsersManagement view
        public IActionResult UsersManagement()
        {
            var users = _context.Users
                .Where(u => u.Role != "Admin" && u.IsDeleted==false) // Exclude admin users
                .ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationEmail(int id)
        {
            // Find the user by ID
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UsersManagement");
            }

            // Check if the user's email is already verified
            if (user.IsEmailVerified)
            {
                TempData["Info"] = "This user's email is already verified.";
                return RedirectToAction("UsersManagement");
            }

            // Generate a new verification token
            var verificationToken = Guid.NewGuid().ToString();

            // Update the user's verification token
            user.EmailVerificationToken = verificationToken;

            // Save changes to the database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Generate the verification link
            var verificationLink = Url.Action("VerifyEmail", "User", new { token = verificationToken }, Request.Scheme);

            try
            {
                // Create a more professional email body with HTML formatting
                var emailBody = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background-color: #4a6fdc; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 5px 5px; }}
                .button {{ display: inline-block; background-color: #4a6fdc; color: white; text-decoration: none; padding: 10px 20px; border-radius: 5px; margin-top: 20px; }}
                .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h2>Email Verification</h2>
                </div>
                <div class='content'>
                    <p>Dear {user.FullName},</p>
                    <p>Your account has been created, but you need to verify your email address to activate your account.</p>
                    <p>Please click the button below to verify your email address:</p>
                    <p style='text-align: center;'>
                        <a href='{verificationLink}' class='button'>Verify Email Address</a>
                    </p>
                    <p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
                    <p>{verificationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>Thank you,<br>The Property Management Team</p>
                </div>
                <div class='footer'>
                    <p>If you did not create an account, please ignore this email or contact support.</p>
                </div>
            </div>
        </body>
        </html>";

                // Send the email with the verification link
                await _emailService.SendEmailAsync(user.Email, "Email Verification", emailBody);

                // Set success message
                TempData["Success"] = $"Verification email sent to {user.Email} successfully.";
            }
            catch (Exception ex)
            {
                // Log the error
                // Set error message
                TempData["Error"] = $"Failed to send verification email: {ex.Message}";
            }

            // Redirect back to the users management page
            return RedirectToAction("UsersManagement");
        }
        public IActionResult ExportUsers()
        {
            // Get only Buyer and Seller users (not Admin)
            var users = _context.Users
                .Where(u => u.Role == "Buyer" || u.Role == "Seller")
                .ToList();

            // Create a new Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the package
                var worksheet = package.Workbook.Worksheets.Add("Users");

                // Add headers
                worksheet.Cells[1, 1].Value = "Full Name";
                worksheet.Cells[1, 2].Value = "Email";
                worksheet.Cells[1, 3].Value = "Contact Number";
                worksheet.Cells[1, 4].Value = "Role";
                worksheet.Cells[1, 5].Value = "Address";
                worksheet.Cells[1, 6].Value = "Created At";

                // Style the header row
                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 120, 215));
                    range.Style.Font.Color.SetColor(Color.White);
                }

                // Add data rows
                int row = 2;
                foreach (var user in users)
                {
                    worksheet.Cells[row, 1].Value = user.FullName;
                    worksheet.Cells[row, 2].Value = user.Email;
                    worksheet.Cells[row, 3].Value = user.ContactNumber;
                    worksheet.Cells[row, 4].Value = user.Role;
                    worksheet.Cells[row, 5].Value = user.Address;

                    // Format the date
                    if (user.CreatedAt.HasValue)
                    {
                        worksheet.Cells[row, 6].Value = user.CreatedAt.Value;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    }

                    // Alternate row coloring for better readability
                    if (row % 2 == 0)
                    {
                        using (var range = worksheet.Cells[row, 1, row, 6])
                        {
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 240));
                        }
                    }

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Set content type and filename for the file
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = $"Users_Export_{DateTime.Now:yyyy-MM-dd}.xlsx";

                // Convert the package to a byte array
                var fileBytes = package.GetAsByteArray();

                // Return the file
                return File(fileBytes, contentType, fileName);
            }
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
                user.IsDeleted = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UsersManagement");
        }

        [HttpGet]
        public async Task<IActionResult> BoostEarnings()
        {
            // Get total earnings from all paid boosts
            decimal totalEarnings = await _context.BoostedProperties
                .Where(bp => bp.PaymentStatus == "Paid")
                .SumAsync(bp => bp.Price);

            // Get count of successful boosts
            int totalBoosts = await _context.BoostedProperties
                .Where(bp => bp.PaymentStatus == "Paid")
                .CountAsync();

            // Create a simple view model
            var viewModel = new BoostEarningsViewModel
            {
                TotalEarnings = totalEarnings,
                BoostCount = totalBoosts
            };

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> ExportBoostEarningsReport()
        {
            try
            {
                // Get all paid boosts with required data
                var boostData = await _context.BoostedProperties
                    .Where(bp => bp.PaymentStatus == "Paid")
                    .Select(bp => new
                    {
                        bp.Id,
                        PropertyId = bp.PropertyId,
                        PropertyTitle = bp.Property.Title,
                        SellerName = bp.Property.Seller.FullName,
                        bp.Hours,
                        bp.Price,
                        StartTime = bp.StartTime,
                        EndTime = bp.EndTime,
                        BoostPeriod = $"{bp.StartTime.ToString("yyyy-MM-dd HH:mm")} to {bp.EndTime.ToString("yyyy-MM-dd HH:mm")}"
                    })
                    .OrderByDescending(b => b.StartTime)
                    .ToListAsync();

                // Create a new Excel workbook
                using (var workbook = new XLWorkbook())
                {
                    // Add a worksheet
                    var worksheet = workbook.Worksheets.Add("Boost Earnings");

                    // Add headers
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Property";
                    worksheet.Cell(1, 3).Value = "Seller";
                    worksheet.Cell(1, 4).Value = "Boost Period";
                    worksheet.Cell(1, 5).Value = "Hours";
                    worksheet.Cell(1, 6).Value = "Amount (Rs.)";
                    worksheet.Cell(1, 7).Value = "Date";

                    // Style the header row
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add data rows
                    int row = 2;
                    foreach (var boost in boostData)
                    {
                        worksheet.Cell(row, 1).Value = boost.Id;
                        worksheet.Cell(row, 2).Value = boost.PropertyTitle;
                        worksheet.Cell(row, 3).Value = boost.SellerName;
                        worksheet.Cell(row, 4).Value = boost.BoostPeriod;
                        worksheet.Cell(row, 5).Value = boost.Hours;
                        worksheet.Cell(row, 6).Value = boost.Price;
                        worksheet.Cell(row, 7).Value = boost.StartTime.ToString("yyyy-MM-dd HH:mm:ss");

                        // Style amount column as currency
                        worksheet.Cell(row, 6).Style.NumberFormat.Format = "Rs.#,##0.00";

                        row++;
                    }

                    // Add a total row
                    worksheet.Cell(row, 5).Value = "Total:";
                    worksheet.Cell(row, 5).Style.Font.Bold = true;
                    worksheet.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    worksheet.Cell(row, 6).FormulaA1 = $"SUM(F2:F{row - 1})";
                    worksheet.Cell(row, 6).Style.Font.Bold = true;
                    worksheet.Cell(row, 6).Style.NumberFormat.Format = "Rs.#,##0.00";
                    worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightGreen;

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Create a border for the data
                    var dataRange = worksheet.Range(1, 1, row, 7);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Convert the Excel workbook to a byte array
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Flush();
                        stream.Position = 0;

                        // Return the Excel file
                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"BoostEarningsReport_{DateTime.Now:yyyyMMdd}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting boost earnings report");
                return RedirectToAction("BoostEarnings", new { error = "Failed to export report. Please try again." });
            }
        }

    }
}
