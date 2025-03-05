using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Propertease.Models
{
    public class Land
    {
        public int ID { get; set; } // Primary Key
        public double? LandArea { get; set; } // In square meters or feet
        public string LandType { get ; set; }
        public string SoilQuality { get ; set; }
        // Foreign Key
        public int PropertyID { get; set; }
        public Properties Properties { get; set; }
    }
}
