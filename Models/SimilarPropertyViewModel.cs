namespace Propertease.Models
{
    // Models/SimilarPropertyViewModel.cs
    public class SimilarPropertyViewModel
    {
        public int? PropertyId { get; set; }
        public string? Title { get; set; }
        public decimal Price { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public double? BuildupArea { get; set; }
        public int? Floors { get; set; }
        public string ImageUrl { get; set; }
        // add other fields as needed
    }
}
