namespace Propertease.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // User who should receive the notification (can be null for global notifications)
        public int? UserId { get; set; }
        public virtual User User { get; set; }

        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public int? PropertyId { get; set; }
        public string NotificationType { get; set; } // e.g., "PropertyApproval", "PropertyRejection"
    }
}