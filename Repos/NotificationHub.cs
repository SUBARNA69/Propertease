using Microsoft.AspNetCore.SignalR;
using Propertease.Repos;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Propertease.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;

        public NotificationHub(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Add these connection management methods
        public override async Task OnConnectedAsync()
        {
            if (Context.User.Identity.IsAuthenticated)
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Automatically load notifications on connection
                await GetUnreadNotifications();
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.User.Identity.IsAuthenticated)
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Existing method, no changes needed
        public async Task GetUnreadNotifications()
        {
            if (Context.User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
                var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
                await Clients.Caller.SendAsync("LoadUnreadNotifications", notifications);
            }
        }

        // Existing method, no changes needed
        public async Task MarkAsRead(int notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId);
        }
    }
}