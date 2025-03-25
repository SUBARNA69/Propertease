using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROPERTEASE.Models
{
    public class BoostViewModel
    {
        public int PropertyId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string PropertyLink { get; set; }
        public int SelectedHours { get; set; } = 12;
        // Removed SelectedPeople property
        public decimal TotalPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        //public string PaymentId { get; set; }

        public List<int> AvailableHours => new List<int> {
            12, 24, 36, 48, 60, 72, 84, 96, 108, 120, 132, 144, 156, 168, 180, 192, 204, 216, 228, 240
        };

        public void CalculateTotalPrice()
        {
            int basePrice = 100;
            int additionalHours = (SelectedHours - 12) / 12 * 50; // Additional cost for every 12 hours
            TotalPrice = basePrice + additionalHours;
            // Removed people cost calculation
        }
    }
}