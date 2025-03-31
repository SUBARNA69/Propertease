using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class BuyerRatingViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public int? ViewingRequestId { get; set; }

        [Required(ErrorMessage = "Please select a rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        public string? Review { get; set; }
    }
}