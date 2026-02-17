namespace DrimAgents.Api.Domain.Chat;

using DrimAgents.Api.Domain.Tasks;

public class ChatSession
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public TaskStage Stage { get; set; }
    public string? ClaudeSessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Task Task { get; set; } = null!;
    public List<ChatMessage> Messages { get; set; } = [];
}
