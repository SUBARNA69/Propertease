using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Propertease.Hubs;
using Propertease.Models;

namespace Propertease.Repos
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string message, int? userId = null, int? propertyId = null);
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ProperteaseDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ProperteaseDbContext context,
                                  IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(string message, int? userId = null, int? propertyId = null)
        {
            // Create and save notification to database
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                PropertyId = propertyId,
                CreatedAt = DateTime.Now
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification to connected users
            if (userId.HasValue)
            {
                // Modified: Send to specific user's group instead of using Clients.User
                await _hubContext.Clients.Group($"User_{userId.ToString()}")
                    .SendAsync("ReceiveNotification", message);
            }
            else
            {
                // For admin notifications or global notifications
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
            }
        }

        // These methods don't need changes
        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}