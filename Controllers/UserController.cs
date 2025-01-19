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

namespace Propertease.Controllers
{

    public class UserController : Controller
    {
        private readonly ProperteaseDbContext UserDbContext;
        private readonly IDataProtector _dataProtector;
        IWebHostEnvironment webHostEnvironment;

        public UserController(ProperteaseDbContext userDbContext,ProperteaseSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment webHostEnvironment)
        {
            this.UserDbContext = userDbContext;
            this._dataProtector = provider.CreateProtector(p.Key);
            this.webHostEnvironment = webHostEnvironment;
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
        public IActionResult Register(AddUser user)
        {
            if (ModelState.IsValid)
            {
                // Check if email is already registered
                if (UserDbContext.Users.Any(u => u.Email.ToUpper() == user.Email.ToUpper()))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(user);
                }

                // Create and save a new user
                var newUser = new User
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    ContactNumber = user.ContactNumber,
                    Role = user.Role,
                    Address = user.Address,
                    Password = _dataProtector.Protect(user.Password) // Encrypt the password
                };

                UserDbContext.Users.Add(newUser);
                UserDbContext.SaveChanges();

                // Redirect to a success page or login page
                return RedirectToAction("Login");
            }

            // If ModelState is invalid, return the same form with validation errors
            return View(user);
        }

      
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(User uEdit)
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
                    // Create claims for the authenticated user
                    List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),  // Using Id instead of FullName
                new Claim(ClaimTypes.Role, u.Role ?? "DefaultRole"),    // Use Role from User model
                new Claim("FullName", u.FullName),                     // Custom claim for FullName
                new Claim(ClaimTypes.Email, u.Email)                   // Correct ClaimType for email
            };

                    // Create a ClaimsIdentity and sign in the user
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

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


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        [Authorize]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Replace with actual method to get the current user ID
            int userId = GetCurrentUserId();

            var user = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                // Handle the case where the user is not found
                return RedirectToAction("Error", new { message = "User not found" });
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
        public async Task<IActionResult> Profile(User user, IFormFile Photo)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // Get the logged-in user ID
            string loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(loggedInUserId) || !int.TryParse(loggedInUserId, out int userId))
            {
                return Unauthorized("User is not logged in.");
            }

            // Fetch the existing user from the database
            var existingUser = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            // Update user details
            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.ContactNumber = user.ContactNumber;
            existingUser.Address = user.Address;

            // Handle profile picture upload
            if (Photo != null && Photo.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Photo.FileName);
                string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string filePath = Path.Combine(uploadFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Photo.CopyToAsync(fileStream);
                }

                // Update the user's profile picture
                existingUser.Image = fileName;
            }

            // Save changes
            UserDbContext.Users.Update(existingUser);
            await UserDbContext.SaveChangesAsync();

            // Redirect to Profile page
            return RedirectToAction("Profile", "User");
        }



        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(Models.ChangePassword c)
        {
            if (ModelState.IsValid)
            {
                var u = UserDbContext.Users.Where(e => e.Id == Convert.ToInt16(User.Identity!.Name)).First();
                if (_dataProtector.Unprotect(u.Password) != c.CurrentPassword)
                {
                    ModelState.AddModelError("", "Check your current password");
                }
                else
                {
                    if (c.NewPassword == c.ConfirmPassword)
                    {
                        u.Password = _dataProtector.Protect(c.NewPassword);
                        UserDbContext.Update(u);
                        UserDbContext.SaveChanges();
                        return Content("Success");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Confirm Password doesnot match");
                        return View(c);
                    }

                }

            }
            return Json("Fail");
            
        }
    }
}
