using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class PropertyComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } // The comment text

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp

        // Foreign key for Property
        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public Properties Property { get; set; } // Navigation property

        // Foreign key for User (who posted the comment)
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; } // Navigation property
    }
}
