using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Propertease.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}