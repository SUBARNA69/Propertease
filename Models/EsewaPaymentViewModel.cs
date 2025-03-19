namespace Propertease.Models
{
    public class EsewaPaymentViewModel
    {
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string TransactionUuid { get; set; }
        public string ProductCode { get; set; }
        public decimal ProductServiceCharge { get; set; }
        public decimal ProductDeliveryCharge { get; set; }
        public string SuccessUrl { get; set; }
        public string FailureUrl { get; set; }
        public string PaymentUrl { get; set; }
        public string SignedFieldNames { get; set; }
        public string Signature { get; set; }
    }
}
