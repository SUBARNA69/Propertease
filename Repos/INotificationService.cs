using Propertease.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Propertease.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(string title, string message, string type, int recipientId, int? relatedPropertyId = null);
        Task<List<Notification>> GetRecentNotificationsForUserAsync(int userId, int count = 10);
        Task<List<Notification>> GetUnreadNotificationsForUserAsync(int userId);
        Task<int> GetUnreadNotificationCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> SendNotificationToUserAsync(int recipientId, Notification notification);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
        Task<bool> DeleteAllNotificationsAsync(int userId);
    }
}