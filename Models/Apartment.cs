﻿using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Propertease.Models
{
    public class Apartment
    {
        public int ID { get; set; } // Primary Key
        public int? Rooms { get; set; }
        public int? Kitchens { get; set; }
        public int? Bathrooms { get; set; }
        public int? SittingRooms { get; set; }
        public double? RoomSize { get; set; } // In square meters or feet
        public DateOnly BuiltYear { get; set; } // e.g., North, South

        // Foreign Key
        public int PropertyID { get; set; }
        public Properties Properties { get; set; }
    }
}
