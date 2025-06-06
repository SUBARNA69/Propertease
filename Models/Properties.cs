﻿using NuGet.Protocol.Plugins;
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
        public string RoadAccess { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ThreeDModel { get; set; } 
        public DateTime? SoldDate { get; set; } = DateTime.Now;
        public int SellerId { get; set; }   // Foreign Key
        public string Status { get; set; }
        public User Seller { get; set; } // Link to the User model
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now; // When the property was first listed

        //public DateTime? SoldDate { get; set; } 
        public ICollection<Apartment> Apartments { get; set; }
        public ICollection<House> Houses { get; set; }
        public ICollection<Land> Lands { get; set; }
        public ICollection<PropertyImage> PropertyImages { get; set; }
        public ICollection<PropertyComment> PropertyComments { get; set; }
        public ICollection<PropertyViewingRequest>? PropertyViewingRequests { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    }
}
