using Propertease.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROPERTEASE.Models
{
    public class BoostedProperty
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public int Hours { get; set; }
        // Removed PeopleToReach property
        public decimal Price { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }

        // Navigation property
        public virtual Properties Property { get; set; }
    }
}