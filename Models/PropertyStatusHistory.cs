using System;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class PropertyStatusHistory
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [Required]
        public string OldStatus { get; set; }

        [Required]
        public string NewStatus { get; set; }

        [Required]
        public DateTime ChangedDate { get; set; } = DateTime.Now;

        public int ChangedById { get; set; }

        public virtual Properties Property { get; set; }
        public virtual User ChangedBy { get; set; }
    }
}

