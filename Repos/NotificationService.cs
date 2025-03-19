using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Propertease.Models;
using Propertease.Hubs;
using Propertease.Repos;
using Propertease.Services;
using Propertease.Controllers;

namespace Propertease.Repos
{
    public class NotificationService : INotificationService
    {
        private readonly ProperteaseDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ProperteaseDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Create a new notification and save it to the database
        public async Task<Notification> CreateNotificationAsync(string title, string message, string type, int recipientId, int? relatedPropertyId = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                RecipientId = recipientId,
                RelatedPropertyId = relatedPropertyId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send the notification to the user if they're online
            await SendNotificationAsync(recipientId, notification);

            return notification;
        }

        // Get recent notifications for a user
        public async Task<List<Notification>> GetNotificationsForUserAsync(int userId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        // Get count of unread notifications for a user
        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .CountAsync();
        }

        // Mark a notification as read
        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Mark all notifications as read for a user
        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return false;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Send a notification to a user via SignalR
        public async Task<bool> SendNotificationAsync(int recipientId, Notification notification)
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
                    createdAt = notification.CreatedAt,
                    relatedPropertyId = notification.RelatedPropertyId
                });

                // Update unread count
                var unreadCount = await GetUnreadNotificationCountAsync(recipientId);
                await _hubContext.Clients.Group($"User_{recipientId}").SendAsync("ReceiveUnreadCount", unreadCount);

                return true;
            }
            catch
            {
                // If sending fails (user offline, etc.), the notification is still saved in the database
                return false;
            }
        }
    }
}

