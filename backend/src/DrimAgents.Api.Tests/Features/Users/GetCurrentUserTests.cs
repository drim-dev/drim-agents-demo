using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;

namespace DrimAgents.Api.Tests.Features.Users;

[Collection(UsersTestsCollection.Name)]
public class GetCurrentUserTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public GetCurrentUserTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_return_current_user_when_authenticated()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            AvatarUrl = "https://example.com/avatar.jpg",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        await _fixture.Database.Save(user);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(
            user.Id,
            user.Email,
            user.Role.ToString());

        var response = await client.GetAsync("/api/users/me", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(Base32Encoder.Encode(user.Id));
        result.Email.Should().Be("test@example.com");
        result.DisplayName.Should().Be("Test User");
        result.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        result.Role.Should().Be("User");
        result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Should_return_user_with_null_optional_fields()
    {
        var user = new User
        {
            Email = "minimal@example.com",
            DisplayName = null,
            AvatarUrl = null,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fixture.Database.Save(user);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(
            user.Id,
            user.Email,
            user.Role.ToString());

        var response = await client.GetAsync("/api/users/me", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(Base32Encoder.Encode(user.Id));
        result.Email.Should().Be("minimal@example.com");
        result.DisplayName.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task Should_return_admin_role_correctly()
    {
        var admin = new User
        {
            Email = "admin@example.com",
            DisplayName = "Admin User",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fixture.Database.Save(admin);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(
            admin.Id,
            admin.Email,
            admin.Role.ToString());

        var response = await client.GetAsync("/api/users/me", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var client = _fixture.HttpClient.CreateClient();

        var response = await client.GetAsync("/api/users/me", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_return_not_found_when_user_does_not_exist_in_database()
    {
        var nonExistentUserId = 999999L;
        var client = _fixture.HttpClient.CreateAuthenticatedClient(
            nonExistentUserId,
            "nonexistent@example.com",
            "User");

        var response = await client.GetAsync("/api/users/me", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_correct_user_when_multiple_users_exist()
    {
        var user1 = new User
        {
            Email = "user1@example.com",
            DisplayName = "User One",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Email = "user2@example.com",
            DisplayName = "User Two",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user3 = new User
        {
            Email = "user3@example.com",
            DisplayName = "User Three",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fixture.Database.Save(user1, user2, user3);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(
            user2.Id,
            user2.Email,
            user2.Role.ToString());

        var response = await client.GetAsync("/api/users/me", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(Base32Encoder.Encode(user2.Id));
        result.Email.Should().Be("user2@example.com");
        result.DisplayName.Should().Be("User Two");
    }

    private record UserResponse(
        string Id,
        string Email,
        string? DisplayName,
        string? AvatarUrl,
        string Role,
        DateTime CreatedAt);
}
