namespace Propertease.Models
{
    public class SoldPropertyViewModel
    {
        public int ViewingRequestId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string PropertyDescription { get; set; }
        public string PropertyType { get; set; }
        public decimal PropertyPrice { get; set; }
        public string PropertyLocation { get; set; }
        public string PropertyImage { get; set; }
        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public DateTime CompletionDate { get; set; }
        public bool HasBeenRated { get; set; }
    }
}