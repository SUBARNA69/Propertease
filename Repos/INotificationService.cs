using Propertease.Models;
using Propertease.Repos;

namespace Propertease.Repos
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(string title, string message, string type, int recipientId, int? relatedPropertyId = null);
        Task<List<Notification>> GetNotificationsForUserAsync(int userId, int count = 10);
        Task<int> GetUnreadNotificationCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> SendNotificationAsync(int recipientId, Notification notification);
    }
}