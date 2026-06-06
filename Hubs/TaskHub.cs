using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TaskManager.API.Hubs
{
    [Authorize]
    public class TaskHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var orgId = Context.User?.FindFirstValue("OrganizationId");
            if (!string.IsNullOrEmpty(orgId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"org-{orgId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var orgId = Context.User?.FindFirstValue("OrganizationId");
            if (!string.IsNullOrEmpty(orgId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org-{orgId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}