using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Propertease.Hubs;
using Propertease.Models;
using Propertease.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Propertease.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ProperteaseDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ProperteaseDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Notification> CreateNotificationAsync(string title, string message, string type, int recipientId, int? relatedPropertyId = null)
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    RecipientId = recipientId,
                    RelatedPropertyId = relatedPropertyId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created notification ID {notification.Id} for user {recipientId}");

                // Send the notification to the user if they're online
                await SendNotificationToUserAsync(recipientId, notification);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating notification for user {recipientId}");
                throw;
            }
        }

        public async Task<List<Notification>> GetRecentNotificationsForUserAsync(int userId, int count = 10)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.RecipientId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recent notifications for user {userId}");
                throw;
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsForUserAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.RecipientId == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread notifications for user {userId}");
                throw;
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.RecipientId == userId && !n.IsRead)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread notification count for user {userId}");
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

                if (notification == null)
                {
                    _logger.LogWarning($"Notification {notificationId} not found for user {userId}");
                    return false;
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Marked notification {notificationId} as read for user {userId}");

                // Update unread count for the user
                var unreadCount = await GetUnreadNotificationCountAsync(userId);
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveUnreadCount", unreadCount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {notificationId} as read for user {userId}");
                throw;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && !n.IsRead)
                    .ToListAsync();

                if (!notifications.Any())
                {
                    _logger.LogInformation($"No unread notifications found for user {userId}");
                    return false;
                }

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Marked all notifications as read for user {userId}");

                // Update unread count for the user
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveUnreadCount", 0);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking all notifications as read for user {userId}");
                throw;
            }
        }

        public async Task<bool> SendNotificationToUserAsync(int recipientId, Notification notification)
        {
            try
            {
                // Send to the user's group
                await _hubContext.Clients.Group($"User_{recipientId}").SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt,
                    relatedPropertyId = notification.RelatedPropertyId
                });

                // Update unread count
                var unreadCount = await GetUnreadNotificationCountAsync(recipientId);
                await _hubContext.Clients.Group($"User_{recipientId}").SendAsync("ReceiveUnreadCount", unreadCount);

                _logger.LogInformation($"Sent notification {notification.Id} to user {recipientId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification {notification.Id} to user {recipientId}");
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

                if (notification == null)
                {
                    _logger.LogWarning($"Notification {notificationId} not found for user {userId}");
                    return false;
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted notification {notificationId} for user {userId}");

                // Update unread count if the notification was unread
                if (!notification.IsRead)
                {
                    var unreadCount = await GetUnreadNotificationCountAsync(userId);
                    await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveUnreadCount", unreadCount);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting notification {notificationId} for user {userId}");
                throw;
            }
        }
    }
}