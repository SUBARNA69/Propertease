using PROPERTEASE.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int BoostedPropertyId { get; set; }
        [ForeignKey("BoostedPropertyId")]
        public virtual BoostedProperty BoostedProperty { get; set; }

        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // "eSewa" or "Khalti"
        public string Status { get; set; } // "Pending", "Success", "Failed"
        public string ReferenceId { get; set; } // Reference ID returned by payment gateway
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
