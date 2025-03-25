namespace Propertease.Models
{
    public class SellerReview
    {
        public int Id { get; set; }
        public string BuyerName { get; set; }
        public int Rating { get; set; }
        public string Review { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
