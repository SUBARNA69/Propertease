using System;
using System.ComponentModel.DataAnnotations;

namespace Propertease.Models
{
    public class PropertyView
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int PropertyId { get; set; }
        public Properties Property { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;
    }
}

