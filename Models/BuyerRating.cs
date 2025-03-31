using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class BuyerRating
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Seller")]
        public int SellerId { get; set; }
        public virtual User Seller { get; set; }

        [ForeignKey("Buyer")]
        public int BuyerId { get; set; }
        public virtual User Buyer { get; set; }

        [ForeignKey("Property")]
        public int PropertyId { get; set; }
        public virtual Properties Property { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Review { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Reference to the viewing request that led to this rating
        [ForeignKey("ViewingRequest")]
        public int? ViewingRequestId { get; set; }
        public virtual PropertyViewingRequest ViewingRequest { get; set; }
    }
}