using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Propertease.Models;
using Propertease.Services;
using Propertease.Repos;

namespace Propertease.Repos
{
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;

        public NotificationHub(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Add user to their own group when they connect
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Get unread notifications count for the user
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(int.Parse(userId));
                await Clients.Caller.SendAsync("ReceiveUnreadCount", unreadCount);
            }

            await base.OnConnectedAsync();
        }

        // Remove user from their group when they disconnect
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Mark notification as read
        public async Task MarkAsRead(int notificationId)
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAsReadAsync(notificationId, int.Parse(userId));

                // Get updated unread count
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(int.Parse(userId));
                await Clients.Caller.SendAsync("ReceiveUnreadCount", unreadCount);
            }
        }

        // Mark all notifications as read
        public async Task MarkAllAsRead()
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAllAsReadAsync(int.Parse(userId));
                await Clients.Caller.SendAsync("ReceiveUnreadCount", 0);
            }
        }
    }
}

