using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class SellerReport
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        public int? PropertyId { get; set; }

        [Required]
        public string ReportReason { get; set; }

        [Required]
        public string ReportDescription { get; set; }

        public string ReporterIp { get; set; }

        public int? ReportedByUserId { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending";

        // Navigation properties
        public virtual User Seller { get; set; }
        public virtual Properties Property { get; set; }
        public virtual User ReportedByUser { get; set; }
    }
}