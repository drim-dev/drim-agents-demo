namespace DrimAgents.Api.Common.AgentGateway;

public interface IAgentGateway
{
    Task SendMessageAsync(string taskId, string chatSessionId, string? claudeSessionId, string content, CancellationToken ct);
    bool IsAvailable { get; }
}
