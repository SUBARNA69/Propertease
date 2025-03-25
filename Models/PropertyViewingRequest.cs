using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class PropertyViewingRequest
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key for Property
        [ForeignKey("Property")]
        public int PropertyId { get; set; }
        public virtual Properties Properties { get; set; } // Navigation Property

        // Foreign Key for Buyer (User)
        [ForeignKey("User")]
        public int BuyerId { get; set; }
        public virtual User Buyer { get; set; } // Navigation Property

        [Required]
        public string BuyerName { get; set; }

        [Required, EmailAddress]
        public string BuyerEmail { get; set; }

        [Required]
        public string BuyerContact { get; set; }

        [Required]
        public DateTime ViewingDate { get; set; }

        [Required]
        public string ViewingTime { get; set; }

        public string Notes { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string? Status { get; set; } // Pending, Approved, Rejected, Completed
    }
}
