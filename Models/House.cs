using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Propertease.Models
{
    public class House
    {
        public int ID { get; set; } // Primary Key
        public int? Bedrooms { get; set; }
        public int? Kitchens { get; set; }
        public int? SittingRooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floors { get; set; }
        public double? LandArea { get; set; } // In square meters or feet
        public double? BuildupArea { get; set; } // In square meters or feet

        public string? FacingDirection { get; set; } // e.g., North, South
        public DateOnly BuiltYear { get; set; } // e.g., North, South

        // Foreign Key
        public int PropertyID { get; set; }
        public Properties Properties { get; set; }
    }
}
