namespace Propertease.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // Can be null if notification is for all users (like admins)
        public int? UserId { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Optional: Reference to related entity
        public int? PropertyId { get; set; }

        // Navigation property (if needed)
        public virtual User User { get; set; }
    }
}
