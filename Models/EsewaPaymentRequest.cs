using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class EsewaPaymentRequest
    {
        public string amount { get; set; }
        public string tax_amount { get; set; }
        public string total_amount { get; set; }
        public string transaction_uuid { get; set; }
        public string product_code { get; set; }
        public string signature { get; set; }
        public string success_url { get; set; }
        public string failure_url { get; set; }
        public string signed_field_names { get; set; }

        [NotMapped] // Exclude from EF
        public string PaymentUrl { get; set; }
    }
}
