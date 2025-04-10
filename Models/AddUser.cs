using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Propertease.Models
{
    public class AddUser
    {
        public int Id { get; set; }

        public string? FullName { get; set; } = null!;

        public string? Email { get; set; } = null!;

        public string? Role { get; set; } = null!;
        [RegularExpression(@"^\+977\d{10}$", ErrorMessage = "Phone number must start with +977 followed by exactly 10 digits")]

        public string? ContactNumber { get; set; }
        public IFormFile? photo { get; set; }
        public string? Password { get; set; } = null!;
        public string? Address { get; set; } = null!;
        public DateTime? CreatedAt { get; set; } = DateTime.Now; // When the user registered

        public string ?EncId { get; set; } = null!;

        public string? EmailToken { get; set; } = null!;

        public ICollection<Properties>? Properties { get; set; } // Properties owned by the seller

    }
}
