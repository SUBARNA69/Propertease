namespace Propertease.Models
{
    public class PropertyDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string ImageUrl { get; set; }
        public string SellerName { get; set; }
        public string SellerContact { get; set; }

        // Specific to House
        public int? Bedrooms { get; set; }
        public int? Kitchens { get; set; }
        public int? SittingRooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floors { get; set; }
        public double? Area { get; set; }
        public string FacingDirection { get; set; }

        // Specific to Apartment
        public int? Rooms { get; set; }
        public double? RoomSize { get; set; }

        // Specific to Land
        public double? LandArea { get; set; }

        public string PropertyType { get; set; } // House, Apartment, Land
    }



}
