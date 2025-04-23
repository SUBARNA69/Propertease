using Microsoft.AspNetCore.Mvc;
using Propertease.Models;
using Propertease.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Microsoft.AspNetCore.Hosting;
using Propertease.Repos;
using Propertease.Repos.Propertease.Services;
using Esri.ArcGISRuntime.Ogc;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Propertease.Controllers
{

    public class UserController : Controller
    {
        private readonly ProperteaseDbContext UserDbContext;
        private readonly EmailService emailService;
        private readonly IDataProtector _dataProtector;
        IWebHostEnvironment webHostEnvironment;


        public UserController(ProperteaseDbContext userDbContext,EmailService emailService, ProperteaseSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment webHostEnvironment)
        {
            this.UserDbContext = userDbContext;
            this._dataProtector = provider.CreateProtector(p.Key);
            this.webHostEnvironment = webHostEnvironment;
            this.emailService = emailService;
        }
        public IActionResult Index()
        {
            return View();
        }
     
        [HttpGet]
        public IActionResult Register()
        {
            // Return an empty AddUser model for the form
            return View(new AddUser());
        }
        [HttpPost]
        public async Task<IActionResult> Register(AddUser user)
        {
            if (ModelState.IsValid)
            {
                if (!IsValidEmail(user.Email))
                {
                    ModelState.AddModelError("Email", "Email must end with @gmail.com or @yahoo.com.");
                    return View(user);
                }
                // Check if email is already registered
                if (UserDbContext.Users.Any(u => u.Email.ToUpper() == user.Email.ToUpper()))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(user);
                }

                // Generate a unique verification token
                var verificationToken = Guid.NewGuid().ToString();

                // Create and save a new user
                var newUser = new User
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    ContactNumber = user.ContactNumber,
                    Role = user.Role,
                    Address = user.Address,
                    Password = _dataProtector.Protect(user.Password), // Encrypt the password
                    EmailVerificationToken = verificationToken, // Store the verification token
                    CreatedAt = DateTime.Now,
                    IsEmailVerified = false // Set email as unverified initially
                };

                UserDbContext.Users.Add(newUser);
                UserDbContext.SaveChanges();

                // Generate the verification link
                var verificationLink = Url.Action("VerifyEmail", "User", new { token = verificationToken }, Request.Scheme);

                // Send the email with the verification link
                var emailBody = $@"
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
            <h2>Email Verification</h2>
        </div>
        <div class='content'>
            <p>Dear {user.FullName},</p>
            <p>Thank you for signing up with <strong>ProperTease</strong>! To complete your registration, please verify your email address by clicking the button below:</p>
            <div class='button-container'>
                <a href='{verificationLink}' class='button'>Verify Email</a>
            </div>
            <p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
            <p><a href='{verificationLink}'>{verificationLink}</a></p>
            <p>This link will expire in 24 hours for security reasons.</p>
            <p>We appreciate your trust in <strong>ProperTease</strong> and look forward to serving you.</p>
            <p>Best regards,<br><strong>The ProperTease Team</strong></p>
        </div>
        <div class='footer'>
            <p>If you did not sign up for an account, please ignore this email or contact support.</p>
        </div>
    </div>
</body>
</html>";
                await emailService.SendEmailAsync(user.Email, "Email Verification", emailBody);

                // Redirect to a confirmation page (you can create a new view for this)
                return RedirectToAction("VerificationSent");
            }

            // If ModelState is invalid, return the same form with validation errors
            return View(user);
        }
        private bool IsValidEmail(string email)
        {
            // Regex pattern to validate email format and specific domains
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@(gmail\.com|yahoo\.com)$";
            return Regex.IsMatch(email, emailPattern);
        }
        [HttpGet]
        public IActionResult VerificationSent()
        {
            return View();
        }
        [HttpGet]
        public IActionResult VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid verification link.");
            }

            var user = UserDbContext.Users.FirstOrDefault(u => u.EmailVerificationToken == token);
            if (user == null)
            {
                return BadRequest("Invalid verification link.");
            }

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null; // Clear the token after successful verification

            UserDbContext.Users.Update(user);
            UserDbContext.SaveChanges();

            // Redirect to login page or show a success message
            return RedirectToAction("Login");
        }


        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            // You can add a message here or just redirect to login directly
            return RedirectToAction("Login", "User");
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(AddUser uEdit)
        {
            // Fetch all users from the database
            var users = UserDbContext.Users.ToList();

            // Check if users exist
            if (users != null && users.Any())
            {
                // Find the user with matching email and password
                var u = users.FirstOrDefault(x =>
                    x.Email.ToUpper() == uEdit.Email.ToUpper() &&
                    _dataProtector.Unprotect(x.Password) == uEdit.Password);

                if (u != null)
                {
                    if (!u.IsEmailVerified)
                    {
                        ModelState.AddModelError("", "Please verify your Email before logging in.");
                        return View();
                    }
                    // Create claims for the authenticated user
                    List<Claim> claims = new()
                    {
                        new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),  // Using Id instead of FullName
                        new Claim(ClaimTypes.Role, u.Role ?? "DefaultRole"),    // Use Role from User model
                        new Claim("FullName", u.FullName),                     // Custom claim for FullName
                        new Claim(ClaimTypes.Email, u.Email)   
                        // Correct ClaimType for email
                    };

                    // Create a ClaimsIdentity and sign in the user
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Add RememberMe logic here
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = uEdit.RememberMe, // Will be true if "Remember Me" is checked
                        ExpiresUtc = DateTime.UtcNow.AddDays(7) // Token valid for 7 days
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        authProperties);


                    // Role-based redirection
                    if (u.Role != null && u.Role.Equals("Seller", StringComparison.OrdinalIgnoreCase))
                    {
                        // Redirect to Seller Dashboard after login
                        return RedirectToAction("Dashboard", "Seller");
                    }
                    else if (u.Role != null && u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        // Redirect to Admin Dashboard after login
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        // Redirect other roles (including DefaultRole) to the Home page
                        return RedirectToAction("Home", "Home");
                    }
                }
                else
                {
                    // Incorrect email or password
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }
            else
            {
                // No users exist in the database
                ModelState.AddModelError("", "No registered users found.");
            }

            // Return to the login view with error
            return View();
        }

        // GET: /User/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /User/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the user by email
            var user = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Email.ToUpper() == model.Email.ToUpper());

            // If user not found, still show success to prevent email enumeration attacks
            if (user == null)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var resetToken = Guid.NewGuid().ToString();

            // Store the token and its expiration time (24 hours from now)
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);

            UserDbContext.Users.Update(user);
            await UserDbContext.SaveChangesAsync();

            // Generate the reset link
            var resetLink = Url.Action("ResetPassword", "User",
                new { token = resetToken, email = user.Email }, Request.Scheme);

            // Send the email with the reset link
            var emailBody = $@"
        <h2>Reset Your Password</h2>
        <p>Please click the link below to reset your password:</p>
        <p><a href='{resetLink}'>Reset Password</a></p>
        <p>If you didn't request a password reset, please ignore this email.</p>
        <p>This link will expire in 24 hours.</p>
    ";

            await emailService.SendEmailAsync(user.Email, "Password Reset Request", emailBody);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // GET: /User/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /User/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Invalid password reset token");
                return View("Error");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: /User/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the user by email
            var user = await UserDbContext.Users.FirstOrDefaultAsync(u =>
                u.Email.ToUpper() == model.Email.ToUpper() &&
                u.PasswordResetToken == model.Token);

            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Check if token is expired
            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Password reset token has expired");
                return View(model);
            }

            // Update the user's password
            user.Password = _dataProtector.Protect(model.Password);

            // Clear the reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            UserDbContext.Users.Update(user);
            await UserDbContext.SaveChangesAsync();

            return RedirectToAction("ResetPasswordConfirmation");
        }

        // GET: /User/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        //[Authorize]
        //public IActionResult Dashboard()
        //{
        //    return RedirectToAction("Index", "Home");
        //}

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Get the current user's ID
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get the user from the database
            var user = await UserDbContext.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Get ratings if the user is a seller
            if (user.Role == "Seller")
            {
                var ratings = await UserDbContext.SellerRatings
                    .Where(r => r.SellerId == userId)
                    .ToListAsync();

                // Calculate average rating
                double averageRating = 0;
                if (ratings.Any())
                {
                    averageRating = ratings.Average(r => r.Rating);
                }

                // Count ratings by star value
                ViewBag.Rating5Count = ratings.Count(r => r.Rating == 5);
                ViewBag.Rating4Count = ratings.Count(r => r.Rating == 4);
                ViewBag.Rating3Count = ratings.Count(r => r.Rating == 3);
                ViewBag.Rating2Count = ratings.Count(r => r.Rating == 2);
                ViewBag.Rating1Count = ratings.Count(r => r.Rating == 1);

                ViewBag.AverageRating = averageRating;
                ViewBag.TotalRatings = ratings.Count;
            }
            if (user.Role == "Buyer")
            {
                var ratings = await UserDbContext.BuyerRatings
                  .Where(r => r.BuyerId == userId)
                  .ToListAsync();
                double averageRating = 0;
                if (ratings.Any())
                {
                    averageRating = ratings.Average(r => r.Rating);
                }

                // Count ratings by star value
                ViewBag.Rating5Count = ratings.Count(r => r.Rating == 5);
                ViewBag.Rating4Count = ratings.Count(r => r.Rating == 4);
                ViewBag.Rating3Count = ratings.Count(r => r.Rating == 3);
                ViewBag.Rating2Count = ratings.Count(r => r.Rating == 2);
                ViewBag.Rating1Count = ratings.Count(r => r.Rating == 1);

                ViewBag.AverageRating = averageRating;
                ViewBag.TotalRatings = ratings.Count;
            }

            return View(user);
        }

        // Helper method to get the current user's ID (replace this with your authentication mechanism)
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }


        [HttpPost]
        public async Task<IActionResult> SaveImage(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest("No file selected.");
            }

            string loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(loggedInUserId) || !int.TryParse(loggedInUserId, out int userId))
            {
                return Unauthorized("User is not logged in.");
            }

            var user = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Save the new image file.
            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
            string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            // Update the image field only.
            user.Image = fileName;

            // Save changes without affecting other fields.
            UserDbContext.Users.Update(user);
            await UserDbContext.SaveChangesAsync();

            return RedirectToAction("Profile", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User model, IFormFile? Photo)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the current user's ID
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get the user from the database
            var user = await UserDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Update user properties
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.ContactNumber = model.ContactNumber;
            user.Address = model.Address;

            // Handle profile image upload only if a new photo is provided
            if (Photo != null && Photo.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(user.Image))
                {
                    var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, "Images", user.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Photo.FileName);
                string filePath = Path.Combine(webHostEnvironment.WebRootPath, "Images", fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Photo.CopyToAsync(fileStream);
                }
                user.Image = fileName;
            }
            // Otherwise, image remains unchanged - no need for additional code here

            // Save changes
            await UserDbContext.SaveChangesAsync();

            // Redirect to profile page
            return RedirectToAction(nameof(Profile));
        }

        // In UserController.cs
        [Authorize]
        [HttpGet]
        public IActionResult UpdatePassword()
        {
            return View(new PasswordChangeModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("User/UpdatePassword")] // Add explicit route
        public async Task<IActionResult> UpdatePassword(PasswordChangeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get the current user ID from claims
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    ModelState.AddModelError("", "User authentication error. Please try logging in again.");
                    return View(model);
                }

                // Find the user in the database
                var user = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found. Please try logging in again.");
                    return View(model);
                }

                // Verify current password
                if (_dataProtector.Unprotect(user.Password) != model.CurrentPassword)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Update the password
                user.Password = _dataProtector.Protect(model.NewPassword);
                UserDbContext.Users.Update(user);
                await UserDbContext.SaveChangesAsync();

                // Add success message
                TempData["SuccessMessage"] = "Your password has been changed successfully.";

                return RedirectToAction("Profile", "User");
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while changing your password. Please try again.");
                return View(model);
            }
        }

        // Add this action to the UserController to display viewed properties
        //[HttpGet]
        //[Authorize]
        //public async Task<IActionResult> ViewedProperties()
        //{
        //    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        //    // Get the user's property views, ordered by most recent
        //    var propertyViews = await UserDbContext.PropertyViews
        //        .Include(pv => pv.Property)
        //            .ThenInclude(p => p.PropertyImages)
        //        .Where(pv => pv.UserId == userId)
        //        .OrderByDescending(pv => pv.ViewedAt)
        //        .ToListAsync();

        //    // Group by property to avoid duplicates, taking the most recent view date
        //    var groupedViews = propertyViews
        //        .GroupBy(pv => pv.PropertyId)
        //        .Select(g => g.OrderByDescending(pv => pv.ViewedAt).First())
        //        .ToList();

        //    return View(groupedViews);
        //}
    }
}
