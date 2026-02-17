using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using TaskStage = DrimAgents.Api.Domain.Tasks.TaskStage;
using DrimAgents.Api.Features.Chat;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Tests.Features.Chat;

[Collection(ChatTestsCollection.Name)]
public class SendChatMessageTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public SendChatMessageTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_create_message_and_session()
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

        var response = await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Hello agent" },
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result.Should().NotBeNull();
        result!.Role.Should().Be("User");
        result.Content.Should().Be("Hello agent");

        var session = await _fixture.Database.Execute(async db =>
            await db.ChatSessions.FirstOrDefaultAsync(s => s.TaskId == task.Id, CreateCancellationToken()));
        session.Should().NotBeNull();
        session!.Stage.Should().Be(TaskStage.Design);

        var message = await _fixture.Database.Execute(async db =>
            await db.ChatMessages.FirstOrDefaultAsync(m => m.ChatSessionId == session.Id, CreateCancellationToken()));
        message.Should().NotBeNull();
        message!.Content.Should().Be("Hello agent");
        message.Role.Should().Be(ChatMessageRole.User);

        _fixture.AgentGateway.GetSentMessages().Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_reuse_existing_session()
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

        var existingSession = new ChatSession
        {
            Id = 5000,
            TaskId = task.Id,
            Stage = TaskStage.Design,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(existingSession);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Another message" },
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var sessionCount = await _fixture.Database.Execute(async db =>
            await db.ChatSessions.CountAsync(s => s.TaskId == task.Id, CreateCancellationToken()));
        sessionCount.Should().Be(1);

        var message = await _fixture.Database.Execute(async db =>
            await db.ChatMessages.FirstOrDefaultAsync(m => m.ChatSessionId == existingSession.Id, CreateCancellationToken()));
        message.Should().NotBeNull();
        message!.Content.Should().Be("Another message");
    }

    [Fact]
    public async Task Should_return_bad_request_for_review_stage()
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
            Stage = TaskStage.Review,
            CreatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(task);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Hello" },
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        var response = await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Hello" },
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_503_when_agent_unavailable()
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

        _fixture.AgentGateway.IsAvailable = false;

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var taskId = Base32Encoder.Encode(task.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/chat/messages",
            new { content = "Hello" },
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var client = _fixture.HttpClient.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tasks/00000000000000g0/chat/messages",
            new { content = "Hello" },
            CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record MessageResponse(string Id, string Role, string Content, DateTime CreatedAt);

    public class ValidatorTests
    {
        private readonly SendChatMessage.RequestValidator _validator = new();

        [Fact]
        public void Should_have_error_when_content_is_empty()
        {
            var request = new SendChatMessage.Request(1, 1, "");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorCode("chat:message:content:required");
        }

        [Fact]
        public void Should_have_error_when_content_is_too_long()
        {
            var request = new SendChatMessage.Request(1, 1, new string('a', 50001));
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorCode("chat:message:content:too_long");
        }

        [Fact]
        public void Should_not_have_error_when_content_is_valid()
        {
            var request = new SendChatMessage.Request(1, 1, "Valid message content");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }
    }
}
