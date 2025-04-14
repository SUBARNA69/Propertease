namespace Propertease.Models
{
    public class PropertyDetailsViewModel
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string SellerImage { get; set; }
        public List<string> ImageUrl { get; set; } // This should be a property

        public string SellerName { get; set; }
        public string Status { get; set; }
        public string SellerContact { get; set; }
        public string SellerAddress { get; set; }
        public string SellerEmail { get; set; }


        // Specific to House
        public int? Bedrooms { get; set; }
        public int? Kitchens { get; set; }
        public int? SittingRooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floors { get; set; }
        public string FacingDirection { get; set; }

        // Specific to Apartment
        public int? Rooms { get; set; }
        public double? RoomSize { get; set; }

        // Specific to Land 
        public double? LandArea { get; set; }
        public string? RoadAccess { get; set; }
        public string? LandType { get; set; }
        public string? SoilQuality { get; set; }
        public double? BuildupArea { get; set; } // In square meters or feet
        public DateOnly BuiltYear { get; set; } // In square meters or feet
        public DateTime? CreatedAt { get; set; } // In square meters or feet
        public string PropertyType { get; set; } // House, Apartment, Land
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal? PredictedPrice { get; set; }

        public string? ThreeDModel { get; set; }
        // Add these properties to PropertyDetailsViewMode
        // Add this property to your existing PropertyDetailsViewModel class
        public bool IsFavorite { get; set; }
        public DateTime DateAdded { get; set; } // For showing when a property was added to favorites
        public double SellerAverageRating { get; set; }
        public int SellerTotalRatings { get; set; }
        public List<SellerReview> SellerReviews { get; set; }
        public List<SellerPastProperty> SellerPastProperties { get; set; }
        public List<SimilarProperty> SimilarProperties { get; set; } = new List<SimilarProperty>();

    }
    public class SimilarProperty
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string District { get; set; }
        public string City { get; set; }
    }
}
