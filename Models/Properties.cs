using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class Properties
    {
        public int Id { get; set; }
        // Property Details
        [Required]
        public string PropertyType { get; set; } // e.g., Apartment, House, Land
        public string Title { get; set; }

        public decimal Price { get; set; }

        public string Description { get; set; }

        // Location Details
        [Required]
        public string District { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]

        // Media Upload (Paths or URLs can be stored here, depending on how files are handled)
        public string? Photo { get; set; } 

        public string? ThreeDModel { get; set; } // You can modify this to store path or URL for 3D model if applicable

        public int SellerId { get; set; }   // Foreign Key

        public string Status { get; set; }
        public User Seller { get; set; } // Link to the User model
        public ICollection<Apartment> Apartments { get; set; }
        public ICollection<House> Houses { get; set; }
        public ICollection<Land> Lands { get; set; }
    }
}
