using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Propertease.Models;

namespace Propertease.Models
{
    public class ForumComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key for User
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; } // Navigation property

        // Foreign key for ForumPost
        public int ForumPostId { get; set; }
        [ForeignKey("ForumPostId")]
        public ForumPost ForumPost { get; set; } // Navigation property

    }
}
