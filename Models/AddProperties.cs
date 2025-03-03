using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class AddProperties
    {
        public int PropertyId { get; set; }
        // Property Details
        [Required]
        public string Title { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; }

        [Required]
        public string Description { get; set; }
        public string PropertyType { get; set; }

        // Location Details
        [Required]
        public string District { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]

        // Media Upload (Paths or URLs can be stored here, depending on how files are handled)
        public List<IFormFile> photo { get; set; } // You can modify this to store paths or a collection of file paths if needed

        public string? ThreeDModel { get; set; } // You can modify this to store path or URL for 3D model if applicable

        // Contact Information
        [Required]
        [StringLength(100)]
        public string? Status { get; set; }
        public User Seller { get; set; } // Link to the User model
        public int? HouseID { get; set; } // Primary Key
        public int? Bedrooms { get; set; }
        public int? Kitchens { get; set; }
        public int SittingRooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floors { get; set; }
        public double? Area { get; set; } // In square meters or feet
        public string? FacingDirection { get; set; } // e.g., North, South
        public int? ApartmentID { get; set; } // Primary Key
        public int? Rooms { get; set; }
        public double? RoomSize { get; set; } // In square meters or feet
        // Foreign Key
    }
}
