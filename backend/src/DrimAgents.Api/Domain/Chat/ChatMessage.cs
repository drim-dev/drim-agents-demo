namespace DrimAgents.Api.Domain.Chat;

public class ChatMessage
{
    public long Id { get; set; }
    public long ChatSessionId { get; set; }
    public ChatMessageRole Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public ChatSession ChatSession { get; set; } = null!;
}
