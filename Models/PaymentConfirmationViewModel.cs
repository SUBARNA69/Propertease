namespace Propertease.Models
{
    public class PaymentConfirmationViewModel
    {
        public int BoostId { get; set; }
        public string TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PropertyName { get; set; }
        public string BoostType { get; set; }
        public bool IsSuccessful { get; set; } = true;

        // Formatted properties for easy display
        public string FormattedAmount => Amount.ToString("C2");
        public string FormattedPaymentDate => PaymentDate.ToString("f");
    }
}
