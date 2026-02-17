namespace DrimAgents.Api.Common.AgentGateway;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Database;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class WebSocketAgentGateway : IAgentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private WebSocket? _agentSocket;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdFactory _idFactory;
    private readonly ILogger<WebSocketAgentGateway> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocketAgentGateway(
        IHubContext<ChatHub> chatHub,
        IServiceScopeFactory scopeFactory,
        IIdFactory idFactory,
        ILogger<WebSocketAgentGateway> logger)
    {
        _chatHub = chatHub;
        _scopeFactory = scopeFactory;
        _idFactory = idFactory;
        _logger = logger;
    }

    public bool IsAvailable => _agentSocket is { State: WebSocketState.Open };

    public async Task SendMessageAsync(string taskId, string chatSessionId, string? claudeSessionId, string content, CancellationToken ct)
    {
        if (_agentSocket is not { State: WebSocketState.Open })
            throw new InvalidOperationException("Agent daemon is not connected");

        var payload = JsonSerializer.Serialize(new
        {
            type = "send_message",
            taskId,
            chatSessionId,
            claudeSessionId,
            content
        }, JsonOptions);

        var bytes = Encoding.UTF8.GetBytes(payload);

        await _sendLock.WaitAsync(ct);
        try
        {
            await _agentSocket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        if (_agentSocket is { State: WebSocketState.Open })
        {
            _logger.LogWarning("Closing previous agent daemon connection");
            await _agentSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Replaced by new connection", CancellationToken.None);
        }

        _agentSocket = webSocket;
        _logger.LogInformation("Agent daemon connected");

        var buffer = new byte[4096];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessIncomingMessage(json);
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "Agent daemon WebSocket error");
        }
        finally
        {
            if (_agentSocket == webSocket)
                _agentSocket = null;

            _logger.LogInformation("Agent daemon disconnected");
        }
    }

    private async Task ProcessIncomingMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();
            var taskId = root.GetProperty("taskId").GetString()!;

            var group = _chatHub.Clients.Group($"task:{taskId}");

            switch (type)
            {
                case "stream_started":
                    await group.SendAsync("StreamStarted", new { taskId });
                    break;

                case "stream_token":
                    var token = root.GetProperty("token").GetString();
                    await group.SendAsync("StreamTokenReceived", new { taskId, token });
                    break;

                case "stream_completed":
                    var completedContent = root.GetProperty("content").GetString()!;
                    var claudeSessionId = root.TryGetProperty("claudeSessionId", out var csid)
                        ? csid.GetString()
                        : null;
                    var chatSessionId = root.GetProperty("chatSessionId").GetString()!;

                    var messageId = await SaveAgentMessage(chatSessionId, claudeSessionId, completedContent);

                    await group.SendAsync("StreamCompleted", new { taskId });
                    await group.SendAsync("MessageReceived", new
                    {
                        taskId,
                        message = new
                        {
                            id = messageId,
                            role = ChatMessageRole.Agent.ToString(),
                            content = completedContent,
                            createdAt = DateTime.UtcNow
                        }
                    });
                    break;

                case "stream_error":
                    var error = root.TryGetProperty("error", out var errProp)
                        ? errProp.GetString()
                        : "Unknown error";
                    await group.SendAsync("StreamError", new { taskId, error });
                    break;

                default:
                    _logger.LogWarning("Unknown message type from agent daemon: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent daemon message");
        }
    }

    private async Task<string> SaveAgentMessage(string chatSessionId, string? claudeSessionId, string content)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sessionId = Base32Encoder.Decode(chatSessionId);

        if (claudeSessionId != null)
        {
            var session = await db.ChatSessions.FirstAsync(s => s.Id == sessionId);
            session.ClaudeSessionId = claudeSessionId;
        }

        var message = new ChatMessage
        {
            Id = _idFactory.CreateId(),
            ChatSessionId = sessionId,
            Role = ChatMessageRole.Agent,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();

        return Base32Encoder.Encode(message.Id);
    }
}
