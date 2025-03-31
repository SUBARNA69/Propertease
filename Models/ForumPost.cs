using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class ForumPost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }
        public string? AudioFile { get; set; } // Store Base64 or file path

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key for User
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; } // Navigation property

        public ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>(); // List of comments
    }
}
