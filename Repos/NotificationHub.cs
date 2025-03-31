using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Propertease.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Propertease.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(
            INotificationService notificationService,
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    int userIdInt = int.Parse(userId);

                    // Add user to their own group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                    _logger.LogInformation($"User {userId} connected with connection ID {Context.ConnectionId}");

                    // Send unread count to the user
                    var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(userIdInt);
                    await Clients.Caller.SendAsync("ReceiveUnreadCount", unreadCount);

                    // Send recent unread notifications
                    var notifications = await _notificationService.GetUnreadNotificationsForUserAsync(userIdInt);
                    foreach (var notification in notifications)
                    {
                        await Clients.Caller.SendAsync("ReceiveNotification", new
                        {
                            id = notification.Id,
                            title = notification.Title,
                            message = notification.Message,
                            type = notification.Type,
                            isRead = notification.IsRead,
                            createdAt = notification.CreatedAt,
                            relatedPropertyId = notification.RelatedPropertyId
                        });
                    }
                }
                else
                {
                    _logger.LogWarning($"User ID not found in claims for connection {Context.ConnectionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnConnectedAsync for connection {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Remove user from their group
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                    _logger.LogInformation($"User {userId} disconnected with connection ID {Context.ConnectionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnDisconnectedAsync for connection {Context.ConnectionId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task MarkAsRead(int notificationId)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    int userIdInt = int.Parse(userId);
                    await _notificationService.MarkAsReadAsync(notificationId, userIdInt);
                    _logger.LogInformation($"User {userId} marked notification {notificationId} as read");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification as read: {ex.Message}");
            }
        }

        public async Task MarkAllAsRead()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    int userIdInt = int.Parse(userId);
                    await _notificationService.MarkAllAsReadAsync(userIdInt);
                    _logger.LogInformation($"User {userId} marked all notifications as read");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking all notifications as read: {ex.Message}");
            }
        }
    }
}