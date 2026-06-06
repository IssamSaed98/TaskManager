using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Hubs;

namespace TaskManager.API.Services
{
    public class RealtimeService
    {
        private readonly IHubContext<TaskHub> _hubContext;

        public RealtimeService(IHubContext<TaskHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOrganization(int orgId, string eventName, object data)
        {
            await _hubContext.Clients.Group($"org-{orgId}").SendAsync(eventName, data);
        }

        public async Task NotifyTaskAdded(int orgId, object task)
        {
            await NotifyOrganization(orgId, "TaskAdded", task);
        }

        public async Task NotifyTaskUpdated(int orgId, object task)
        {
            await NotifyOrganization(orgId, "TaskUpdated", task);
        }

        public async Task NotifyTaskDeleted(int orgId, int taskId)
        {
            await NotifyOrganization(orgId, "TaskDeleted", taskId);
        }

        public async Task NotifyEventCreated(int orgId, object ev)
        {
            await NotifyOrganization(orgId, "EventCreated", ev);
        }

        public async Task NotifyEventResponseUpdated(int orgId, object response)
        {
            await NotifyOrganization(orgId, "EventResponseUpdated", response);
        }

        public async Task NotifyEventApproved(int orgId, object approval)
        {
            await NotifyOrganization(orgId, "EventApproved", approval);
        }
    }
}