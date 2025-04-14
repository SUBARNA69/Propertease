namespace Propertease.Models.DTO
{
    public class RecommendationInput
    {
        public float Area { get; set; }
        public int Bedrooms { get; set; }
        public int BuiltYear { get; set; }
        public int Floors { get; set; }

        public List<Dictionary<string, object>> Recommendations { get; set; }
    }

}
