using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class User
    {
        // Required properties
        public int Id { get; set; }

        public string? FullName { get; set; }

        [Required]
        [EmailAddress]

        public string? Email { get; set; }

        [Required]
        [Phone]
        public string? ContactNumber { get; set; }

        // Optional properties
        public string? Image { get; set; }
        public string? Role { get; set; }

        // Password and Confirm Password
        public string? Password { get; set; }

        public string? Address {  get; set; }
        [NotMapped]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
        public string? EmailVerificationToken { get; set; } // Stores the verification code sent via SMS
        public bool IsEmailVerified { get; set; } = false; // Tracks whether the phone number is verified
                                                           // Add these properties to your User model
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now; // When the user registered
        public ICollection<Properties>? Properties { get; set; } // Properties owned by the seller
        public ICollection<ForumPost>? ForumPosts { get; set; }
        public ICollection<ForumComment>? ForumComments { get; set; }
        public ICollection<PropertyComment>? PropertyComments { get; set; }
        public ICollection<PropertyViewingRequest>? PropertyViewingRequests { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
