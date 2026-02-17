using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskStage = DrimAgents.Api.Domain.Tasks.TaskStage;

namespace DrimAgents.Api.Tests.Features.Chat;

[Collection(WebSocketTestsCollection.Name)]
public class AgentWebSocketTests : IAsyncLifetime
{
    private readonly WebSocketTestFixture _fixture;

    public AgentWebSocketTests(WebSocketTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_accept_connection_with_valid_api_key()
    {
        var wsClient = _fixture.Factory.Server.CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(
            new Uri("ws://localhost/ws/agent?apiKey=test-api-key"),
            CreateCancellationToken());

        ws.State.Should().Be(WebSocketState.Open);

        ws.Abort();
    }

    [Fact]
    public async Task Should_reject_connection_with_invalid_api_key()
    {
        var wsClient = _fixture.Factory.Server.CreateWebSocketClient();

        var act = () => wsClient.ConnectAsync(
            new Uri("ws://localhost/ws/agent?apiKey=wrong-key"),
            CreateCancellationToken());

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Should_save_agent_message_on_stream_completed()
    {
        var user = new User
        {
            Email = "ws-test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var project = new Project
        {
            UserId = user.Id,
            Name = "WS Test Project",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = "encrypted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(project);

        var task = new DrimAgents.Api.Domain.Tasks.Task
        {
            ProjectId = project.Id,
            Stage = TaskStage.Design,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(task);

        var session = new ChatSession
        {
            Id = 9000,
            TaskId = task.Id,
            Stage = TaskStage.Design,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(session);

        var encodedTaskId = Base32Encoder.Encode(task.Id);
        var encodedSessionId = Base32Encoder.Encode(session.Id);

        var wsClient = _fixture.Factory.Server.CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(
            new Uri("ws://localhost/ws/agent?apiKey=test-api-key"),
            CreateCancellationToken());

        var payload = JsonSerializer.Serialize(new
        {
            type = "stream_completed",
            taskId = encodedTaskId,
            chatSessionId = encodedSessionId,
            claudeSessionId = "test-session-123",
            content = "Agent response content"
        });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CreateCancellationToken());

        await Task.Delay(TimeSpan.FromSeconds(1));

        var message = await _fixture.Database.Execute(async db =>
            await db.ChatMessages
                .FirstOrDefaultAsync(m => m.ChatSessionId == session.Id && m.Role == ChatMessageRole.Agent,
                    CreateCancellationToken()));
        message.Should().NotBeNull();
        message!.Content.Should().Be("Agent response content");
        message.Role.Should().Be(ChatMessageRole.Agent);

        var updatedSession = await _fixture.Database.Execute(async db =>
            await db.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == session.Id, CreateCancellationToken()));
        updatedSession.Should().NotBeNull();
        updatedSession!.ClaudeSessionId.Should().Be("test-session-123");

        ws.Abort();
    }
}
