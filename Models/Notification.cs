using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string Type { get; set; } // "PropertyListed", "PropertyApproved", "PropertyRejected", etc.

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public int RecipientId { get; set; } // User ID of the recipient

        public int? RelatedPropertyId { get; set; } 

        // Navigation properties
        [ForeignKey("RecipientId")]
        public virtual User Recipient { get; set; }

        [ForeignKey("RelatedPropertyId")]
        public virtual Properties RelatedProperty { get; set; }
    }
}