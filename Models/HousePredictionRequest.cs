namespace Propertease.Models
{
    public class HousePredictionRequest
    {
        // Only include fields from your dataset
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Floors { get; set; }
        public double LotArea { get; set; }
        public double HouseArea { get; set; }  // BuildupArea in your database
        public DateOnly BuiltYear { get; set; }
    }
}
