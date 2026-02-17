using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using TaskStage = DrimAgents.Api.Domain.Tasks.TaskStage;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;

namespace DrimAgents.Api.Tests.Features.Chat;

[Collection(ChatTestsCollection.Name)]
public class GetChatMessagesTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public GetChatMessagesTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_return_messages_for_current_stage()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var project = new Project
        {
            UserId = user.Id,
            Name = "Test Project",
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
            Id = 6000,
            TaskId = task.Id,
            Stage = TaskStage.Design,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(session);

        var message1 = new ChatMessage
        {
            Id = 7000,
            ChatSessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = "First message",
            CreatedAt = DateTime.UtcNow
        };
        var message2 = new ChatMessage
        {
            Id = 7001,
            ChatSessionId = session.Id,
            Role = ChatMessageRole.Agent,
            Content = "Second message",
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(message1, message2);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.GetAsync(
            $"/api/tasks/{taskId}/chat/messages",
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatMessagesResponse>();
        result.Should().NotBeNull();
        result!.ChatSessionId.Should().Be(Base32Encoder.Encode(session.Id));
        result.Stage.Should().Be("Design");
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Content.Should().Be("First message");
        result.Messages[1].Content.Should().Be("Second message");
    }

    [Fact]
    public async Task Should_return_messages_for_specified_stage()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var project = new Project
        {
            UserId = user.Id,
            Name = "Test Project",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = "encrypted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(project);

        var task = new DrimAgents.Api.Domain.Tasks.Task
        {
            ProjectId = project.Id,
            Stage = TaskStage.Plan,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(task);

        var designSession = new ChatSession
        {
            Id = 6100,
            TaskId = task.Id,
            Stage = TaskStage.Design,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(designSession);

        var message = new ChatMessage
        {
            Id = 7100,
            ChatSessionId = designSession.Id,
            Role = ChatMessageRole.User,
            Content = "Design message",
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(message);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.GetAsync(
            $"/api/tasks/{taskId}/chat/messages?stage=Design",
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatMessagesResponse>();
        result.Should().NotBeNull();
        result!.Stage.Should().Be("Design");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Content.Should().Be("Design message");
    }

    [Fact]
    public async Task Should_return_messages_after_id()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var project = new Project
        {
            UserId = user.Id,
            Name = "Test Project",
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
            Id = 6200,
            TaskId = task.Id,
            Stage = TaskStage.Design,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(session);

        var message1 = new ChatMessage
        {
            Id = 7200,
            ChatSessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = "Old message",
            CreatedAt = DateTime.UtcNow
        };
        var message2 = new ChatMessage
        {
            Id = 7201,
            ChatSessionId = session.Id,
            Role = ChatMessageRole.Agent,
            Content = "New message",
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(message1, message2);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.GetAsync(
            $"/api/tasks/{taskId}/chat/messages?afterId={message1.Id}",
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatMessagesResponse>();
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(1);
        result.Messages[0].Content.Should().Be("New message");
    }

    [Fact]
    public async Task Should_return_empty_when_no_session()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var project = new Project
        {
            UserId = user.Id,
            Name = "Test Project",
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

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.GetAsync(
            $"/api/tasks/{taskId}/chat/messages",
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatMessagesResponse>();
        result.Should().NotBeNull();
        result!.ChatSessionId.Should().BeNull();
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_return_not_found_for_other_users_task()
    {
        var user1 = new User
        {
            Email = "user1@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            Email = "user2@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user1, user2);

        var project = new Project
        {
            UserId = user1.Id,
            Name = "Test Project",
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

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user2.Id, user2.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.GetAsync(
            $"/api/tasks/{taskId}/chat/messages",
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record MessageItem(string Id, string Role, string Content, DateTime CreatedAt);
    private record ChatMessagesResponse(string? ChatSessionId, string Stage, MessageItem[] Messages);
}
