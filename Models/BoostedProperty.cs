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
        public DateTime StartTime { get; set; } = DateTime.UtcNow.AddMinutes(345);
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddMinutes(345);
        public bool IsActive { get; set; }

        [StringLength(50)]
        public string? TransactionId { get; set; } // eSewa's reference ID
        public string? TransactionCode { get; set; } // eSewa's reference ID

        public string PaymentStatus { get; set; } = "Pending"; // Pending/Completed/Failed
        public string? TransactionUuid { get; set; } // Make sure this field exists
        public DateTime? PaymentDate { get; set; }
        public virtual Properties Property { get; set; }
    }
}