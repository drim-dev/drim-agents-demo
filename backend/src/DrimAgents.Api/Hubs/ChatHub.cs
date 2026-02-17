namespace DrimAgents.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    public async Task JoinTask(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task:{taskId}");
    }

    public async Task LeaveTask(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task:{taskId}");
    }
}
