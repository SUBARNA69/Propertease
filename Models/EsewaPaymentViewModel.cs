namespace Propertease.Models
{
    public class EsewaPaymentViewModel
    {
        public string Amount { get; set; }
        public string TaxAmount { get; set; }
        public string TotalAmount { get; set; }
        public string TransactionUuid { get; set; }
        public string ProductCode { get; set; }
        public string ProductServiceCharge { get; set; }
        public string Signature { get; set; }
        public string SuccessUrl { get; set; }
        public string FailureUrl { get; set; }
        public string SignedFieldNames { get; set; }
        public string PaymentUrl { get; set; }
    }
}
