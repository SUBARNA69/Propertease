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
            var users = UserDbContext.Users.ToList();
            if (users != null)
            {

                var u = users.Where(x => x.Email.ToUpper().Equals(uEdit.Email.ToUpper()) && _dataProtector.Unprotect(x.Password).Equals(uEdit.Password)).FirstOrDefault();
                if (u != null)
                {
                    List<Claim> claims = new()
                        {
                            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),  // Using Id instead of FullName
                            new Claim(ClaimTypes.Role, u.Role ?? "DefaultRole"),  // Use Role from User model
                            new Claim("FullName", u.FullName),  // Custom claim for FullName
                            new Claim(ClaimTypes.Email, u.Email)  // Correct ClaimType for email
                        };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return RedirectToAction("Home", "Home");

                }
            }
            else
            {
                ModelState.AddModelError("", "Incorrect password");
            }
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

        [HttpGet]

        [HttpPost]
        public async Task<IActionResult> SaveImage(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest("No file selected.");
            }

            try
            {
                // Get the logged-in user ID as a string
                string loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(loggedInUserId))
                {
                    return Unauthorized("User is not logged in.");
                }

                // Convert loggedInUserId to an integer
                if (!int.TryParse(loggedInUserId, out int userId))
                {
                    return BadRequest("Invalid user ID.");
                }

                // Generate a unique file name
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);

                // Define the path to save the image
                string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "Images");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string filePath = Path.Combine(uploadFolder, fileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                // Update the user image in the database
                var user = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                user.Image = fileName;
                UserDbContext.Users.Update(user);
                await UserDbContext.SaveChangesAsync();

                // Redirect to the Profile action
                return RedirectToAction("Profile", "User");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            // Get the logged-in user ID
            string loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(loggedInUserId) || !int.TryParse(loggedInUserId, out int userId))
            {
                return Unauthorized("User is not logged in.");
            }

            // Fetch user details from the database
            var user = await UserDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(User user)
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

            // Update fields
            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.ContactNumber = user.ContactNumber;
            existingUser.Address = user.Address;

            // Save changes
            UserDbContext.Users.Update(existingUser);
            await UserDbContext.SaveChangesAsync();

            // Redirect to profile page after successful update
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
