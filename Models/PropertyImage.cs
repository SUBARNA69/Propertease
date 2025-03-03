using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class PropertyImage
    {
        [Key]
        public int Id { get; set; }

        public string? Photo { get; set; } // Stores the image path or URL

        // Foreign key linking to Properties model
        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public Properties Property { get; set; }
    }
}
