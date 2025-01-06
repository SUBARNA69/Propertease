using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class User
    {
        // Required properties
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string ContactNumber { get; set; }

        // Optional properties
        public string? Image { get; set; }
        public string? Role { get; set; }

        // Password and Confirm Password
        [Required]
        [StringLength(100, MinimumLength = 6)]  // Minimum 6 characters for password
        public string Password { get; set; }

        public string? Address {  get; set; }
        [NotMapped]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

    }
}
