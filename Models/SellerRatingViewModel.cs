using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class SellerRatingViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; }

        public int SellerId { get; set; }
        public string SellerName { get; set; }

        public int? ViewingRequestId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5 stars.")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Review cannot exceed 500 characters.")]
        public string? Review { get; set; }
    }
}