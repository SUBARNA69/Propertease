//namespace Propertease.Models
//{
//    public class PaymentTransaction
//    {
//        public string Id { get; set; } = Guid.NewGuid().ToString();
//        public int PropertyId { get; set; }
//        public int UserId { get; set; }
//        public decimal Amount { get; set; }
//        public int Hours { get; set; }
//        public string Status { get; set; } = "Pending";
//        public string ReferenceId { get; set; }
//        public DateTime CreatedDate { get; set; } = DateTime.UtcNow.AddMinutes(345);
//        public DateTime? CompletedDate { get; set; }

//        // Navigation properties
//        public virtual Properties Property { get; set; }
//        public virtual User User { get; set; }
//    }
//}
