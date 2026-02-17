namespace DrimAgents.Api.Common.AgentGateway;

public class NullAgentGateway : IAgentGateway
{
    public bool IsAvailable => false;

    public Task SendMessageAsync(string taskId, string chatSessionId, string? claudeSessionId, string content, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
