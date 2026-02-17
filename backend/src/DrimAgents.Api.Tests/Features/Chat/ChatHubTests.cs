using System.Net.Http.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using TaskStage = DrimAgents.Api.Domain.Tasks.TaskStage;

namespace DrimAgents.Api.Tests.Features.Chat;

[Collection(ChatTestsCollection.Name)]
public class ChatHubTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private readonly List<HubConnection> _connections = [];

    public ChatHubTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());

    public async Task DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }
    }

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    private HubConnection CreateHubConnection(long userId, string email)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => _fixture.Factory.Server.CreateHandler();
                options.Headers.Add("X-User-Id", userId.ToString());
                options.Headers.Add("X-User-Email", email);
                options.Headers.Add("X-User-Role", "User");
            })
            .Build();

        _connections.Add(connection);
        return connection;
    }

    private async Task<(User User, Project Project, DrimAgents.Api.Domain.Tasks.Task Task)> SetupTestData()
    {
        var user = new User
        {
            Email = "hub-test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var project = new Project
        {
            UserId = user.Id,
            Name = "Hub Test Project",
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

        return (user, project, task);
    }

    [Fact]
    public async Task Should_receive_message_after_join_task()
    {
        var (user, _, task) = await SetupTestData();
        var taskId = Base32Encoder.Encode(task.Id);

        var connection = CreateHubConnection(user.Id, user.Email);
        await connection.StartAsync(CreateCancellationToken());

        var receivedTcs = new TaskCompletionSource<object>();
        connection.On<object>("MessageReceived", msg =>
        {
            receivedTcs.TrySetResult(msg);
        });

        await connection.InvokeAsync("JoinTask", taskId, CreateCancellationToken());

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Hello from hub test" },
            CreateCancellationToken());

        var completed = await Task.WhenAny(receivedTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(receivedTcs.Task, "client should receive MessageReceived event");
    }

    [Fact]
    public async Task Should_not_receive_message_after_leave_task()
    {
        var (user, _, task) = await SetupTestData();
        var taskId = Base32Encoder.Encode(task.Id);

        var connection = CreateHubConnection(user.Id, user.Email);
        await connection.StartAsync(CreateCancellationToken());

        var received = false;
        connection.On<object>("MessageReceived", _ =>
        {
            received = true;
        });

        await connection.InvokeAsync("JoinTask", taskId, CreateCancellationToken());
        await connection.InvokeAsync("LeaveTask", taskId, CreateCancellationToken());

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Should not be received" },
            CreateCancellationToken());

        await Task.Delay(TimeSpan.FromSeconds(1));
        received.Should().BeFalse();
    }

    [Fact]
    public async Task Should_receive_message_on_multiple_clients()
    {
        var (user, _, task) = await SetupTestData();
        var taskId = Base32Encoder.Encode(task.Id);

        var connection1 = CreateHubConnection(user.Id, user.Email);
        var connection2 = CreateHubConnection(user.Id, user.Email);
        await connection1.StartAsync(CreateCancellationToken());
        await connection2.StartAsync(CreateCancellationToken());

        var received1 = new TaskCompletionSource<object>();
        var received2 = new TaskCompletionSource<object>();

        connection1.On<object>("MessageReceived", msg => received1.TrySetResult(msg));
        connection2.On<object>("MessageReceived", msg => received2.TrySetResult(msg));

        await connection1.InvokeAsync("JoinTask", taskId, CreateCancellationToken());
        await connection2.InvokeAsync("JoinTask", taskId, CreateCancellationToken());

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Broadcast message" },
            CreateCancellationToken());

        var bothReceived = Task.WhenAll(received1.Task, received2.Task);
        var completed = await Task.WhenAny(bothReceived, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(bothReceived, "both clients should receive MessageReceived event");
    }
}
