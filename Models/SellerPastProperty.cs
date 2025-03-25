namespace Propertease.Models
{
    public class SellerPastProperty
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; } // "Sold" or "Available"
        public DateTime? ListedDate { get; set; }
    }
}
