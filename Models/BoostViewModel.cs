using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PROPERTEASE.Models
{
    public class BoostViewModel
    {
        public List<int> AvailableHours { get; } = new List<int>
        {
            12, 24, 36, 48, 60, 72, 84, 96, 108, 120, 132, 144, 156, 168, 180, 192, 204, 216, 228, 240
        };

        [Required]
        public int SelectedHours { get; set; } = 12;

        [Required]
        public int SelectedPeople { get; set; } = 1;

        [Required]
        public string FullName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string PropertyLink { get; set; }

        public int TotalPrice { get; set; }

        public void CalculateTotalPrice()
        {
            int basePrice = 100;
            int peopleCost = SelectedPeople * 10; // Changed from 2 to 10 Rs per person
            int additionalHours = (SelectedHours - 12) / 12 * 50; // Additional cost for every 12 hours
            TotalPrice = basePrice + peopleCost + additionalHours;
        }
    }
}