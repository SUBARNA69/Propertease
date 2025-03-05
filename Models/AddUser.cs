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
        public string? ContactNumber { get; set; } = null!;
        public IFormFile? photo { get; set; }
        public string? Password { get; set; } = null!;
        public string? Address { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ?ConfirmPassword { get; set; }
        public string ?EncId { get; set; } = null!;

        public string? EmailToken { get; set; } = null!;

        public ICollection<Properties>? Properties { get; set; } // Properties owned by the seller

    }
}
