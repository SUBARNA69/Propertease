using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class UserRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } // Rating value (1 to 5)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp

        // Foreign key for User being rated
        public int? RatedUserId { get; set; }
        [ForeignKey("RatedUserId")]
        public User? RatedUser { get; set; } // Navigation property

        // Foreign key for User who submitted the rating
        public int? RaterUserId { get; set; }
        [ForeignKey("RaterUserId")]
        public User? RaterUser { get; set; } // Navigation property
    }
}
