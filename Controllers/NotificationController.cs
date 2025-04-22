using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Propertease.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Propertease.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("GetRecentNotifications")]
        public async Task<IActionResult> GetRecentNotifications()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notifications = await _notificationService.GetRecentNotificationsForUserAsync(int.Parse(userId));
                return Json(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications");
                return StatusCode(500, "An error occurred while retrieving notifications");
            }
        }

        [HttpGet("GetUnreadNotifications")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notifications = await _notificationService.GetUnreadNotificationsForUserAsync(int.Parse(userId));
                return Json(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications");
                return StatusCode(500, "An error occurred while retrieving notifications");
            }
        }

        [HttpGet("GetUnreadCount")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var count = await _notificationService.GetUnreadNotificationCountAsync(int.Parse(userId));
                return Json(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count");
                return StatusCode(500, "An error occurred while retrieving notification count");
            }
        }

        [HttpPost("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _notificationService.MarkAsReadAsync(id, int.Parse(userId));
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {id} as read");
                return StatusCode(500, "An error occurred while marking notification as read");
            }
        }

        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _notificationService.MarkAllAsReadAsync(int.Parse(userId));
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "An error occurred while marking all notifications as read");
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _notificationService.DeleteNotificationAsync(id, int.Parse(userId));
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting notification {id}");
                return StatusCode(500, "An error occurred while deleting notification");
            }
        }
        [HttpDelete("DeleteAll")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _notificationService.DeleteAllNotificationsAsync(int.Parse(userId));
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications");
                return StatusCode(500, "An error occurred while deleting all notifications");
            }
        }
    }
}