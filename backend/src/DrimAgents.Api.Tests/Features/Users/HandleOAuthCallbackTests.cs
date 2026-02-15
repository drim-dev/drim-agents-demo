using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Features.Users;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Tests.Features.Users;

[Collection(UsersTestsCollection.Name)]
public class HandleOAuthCallbackTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private readonly HttpClient _client;

    public HandleOAuthCallbackTests(TestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.HttpClient.CreateClient();
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_create_new_user_and_oauth_account_for_new_provider()
    {
        var request = new
        {
            provider = "google",
            providerUserId = "google-123",
            providerEmail = "test@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@example.com");
        result.Role.Should().Be("User");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users
                .Include(u => u.OAuthAccounts)
                .FirstOrDefaultAsync(u => u.Email == "test@example.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
        user.Role.Should().Be(UserRole.User);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        user.OAuthAccounts.Should().HaveCount(1);
        var oauthAccount = user.OAuthAccounts.First();
        oauthAccount.Provider.Should().Be("google");
        oauthAccount.ProviderUserId.Should().Be("google-123");
        oauthAccount.ProviderEmail.Should().Be("test@example.com");
        oauthAccount.IsPrimary.Should().BeTrue();
        oauthAccount.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_return_existing_user_when_oauth_account_exists()
    {
        var existingUser = new User
        {
            Email = "existing@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var existingOAuth = new OAuthAccount
        {
            User = existingUser,
            Provider = "github",
            ProviderUserId = "github-456",
            ProviderEmail = "existing@example.com",
            IsPrimary = true,
            LinkedAt = DateTime.UtcNow.AddDays(-10)
        };

        await _fixture.Database.Save(existingUser, existingOAuth);

        var request = new
        {
            provider = "github",
            providerUserId = "github-456",
            providerEmail = "existing@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("existing@example.com");
        result.Role.Should().Be("User");

        var userCount = await _fixture.Database.Execute(async db =>
            await db.Users.CountAsync(CreateCancellationToken()));
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_link_oauth_account_to_existing_user_with_same_email()
    {
        var existingUser = new User
        {
            Email = "user@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var existingOAuth = new OAuthAccount
        {
            User = existingUser,
            Provider = "google",
            ProviderUserId = "google-789",
            ProviderEmail = "user@example.com",
            IsPrimary = true,
            LinkedAt = DateTime.UtcNow.AddDays(-5)
        };

        await _fixture.Database.Save(existingUser, existingOAuth);

        var request = new
        {
            provider = "github",
            providerUserId = "github-999",
            providerEmail = "user@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("user@example.com");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users
                .Include(u => u.OAuthAccounts)
                .FirstOrDefaultAsync(u => u.Email == "user@example.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.OAuthAccounts.Should().HaveCount(2);
        user.OAuthAccounts.Should().Contain(o => o.Provider == "google" && o.IsPrimary);
        user.OAuthAccounts.Should().Contain(o => o.Provider == "github" && !o.IsPrimary);

        var userCount = await _fixture.Database.Execute(async db =>
            await db.Users.CountAsync(CreateCancellationToken()));
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_allow_login_with_any_linked_oauth_provider()
    {
        var user = new User
        {
            Email = "multi@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var googleOAuth = new OAuthAccount
        {
            User = user,
            Provider = "google",
            ProviderUserId = "google-111",
            ProviderEmail = "multi@example.com",
            IsPrimary = true,
            LinkedAt = DateTime.UtcNow.AddDays(-3)
        };

        var githubOAuth = new OAuthAccount
        {
            User = user,
            Provider = "github",
            ProviderUserId = "github-222",
            ProviderEmail = "multi@example.com",
            IsPrimary = false,
            LinkedAt = DateTime.UtcNow.AddDays(-2)
        };

        var gitlabOAuth = new OAuthAccount
        {
            User = user,
            Provider = "gitlab",
            ProviderUserId = "gitlab-333",
            ProviderEmail = "multi@example.com",
            IsPrimary = false,
            LinkedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _fixture.Database.Save(user, googleOAuth, githubOAuth, gitlabOAuth);

        var request = new
        {
            provider = "gitlab",
            providerUserId = "gitlab-333",
            providerEmail = "multi@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("multi@example.com");

        var userCount = await _fixture.Database.Execute(async db =>
            await db.Users.CountAsync(CreateCancellationToken()));
        userCount.Should().Be(1);

        var oauthCount = await _fixture.Database.Execute(async db =>
            await db.OAuthAccounts.CountAsync(CreateCancellationToken()));
        oauthCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_normalize_email_to_lowercase()
    {
        var request = new
        {
            provider = "google",
            providerUserId = "google-lowercase",
            providerEmail = "Test@Example.COM"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result!.Email.Should().Be("test@example.com");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Should_promote_new_user_to_admin_when_email_in_admin_list()
    {
        // admin@test.com сконфигурирован в appsettings.Development.json
        var request = new
        {
            provider = "google",
            providerUserId = "google-admin-123",
            providerEmail = "admin@test.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.Role.Should().Be("Admin");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Should_promote_existing_user_to_admin_when_email_in_admin_list()
    {
        var existingUser = new User
        {
            Email = "admin@test.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var existingOAuth = new OAuthAccount
        {
            User = existingUser,
            Provider = "google",
            ProviderUserId = "google-existing-admin",
            ProviderEmail = "admin@test.com",
            IsPrimary = true,
            LinkedAt = DateTime.UtcNow.AddDays(-10)
        };

        await _fixture.Database.Save(existingUser, existingOAuth);

        var request = new
        {
            provider = "google",
            providerUserId = "google-existing-admin",
            providerEmail = "admin@test.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.Role.Should().Be("Admin");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Should_promote_to_admin_when_linking_new_oauth_provider()
    {
        var existingUser = new User
        {
            Email = "admin@test.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var existingOAuth = new OAuthAccount
        {
            User = existingUser,
            Provider = "google",
            ProviderUserId = "google-link-admin",
            ProviderEmail = "admin@test.com",
            IsPrimary = true,
            LinkedAt = DateTime.UtcNow.AddDays(-5)
        };

        await _fixture.Database.Save(existingUser, existingOAuth);

        var request = new
        {
            provider = "github",
            providerUserId = "github-link-admin",
            providerEmail = "admin@test.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.Role.Should().Be("Admin");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users
                .Include(u => u.OAuthAccounts)
                .FirstOrDefaultAsync(u => u.Email == "admin@test.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Admin);
        user.OAuthAccounts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_not_promote_user_when_email_not_in_admin_list()
    {
        var request = new
        {
            provider = "google",
            providerUserId = "google-regular-user",
            providerEmail = "regular.user@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/oauth-callback", request, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OAuthCallbackResponse>();
        result.Should().NotBeNull();
        result!.Role.Should().Be("User");

        var user = await _fixture.Database.Execute(async db =>
            await db.Users.FirstOrDefaultAsync(u => u.Email == "regular.user@example.com", CreateCancellationToken()));

        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.User);
    }

    private record OAuthCallbackResponse(string UserId, string Email, string Role);

    public class ValidatorTests
    {
        private readonly HandleOAuthCallback.RequestValidator _validator = new();

        [Fact]
        public void Should_not_have_errors_when_request_is_valid()
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: "google-123",
                ProviderEmail: "test@example.com"
            );

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_have_error_when_provider_empty(string provider)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: provider,
                ProviderUserId: "user-123",
                ProviderEmail: "test@example.com"
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Provider)
                .WithErrorCode("users:auth:provider:required");
        }

        [Theory]
        [InlineData("facebook")]
        [InlineData("twitter")]
        [InlineData("linkedin")]
        [InlineData("invalid")]
        public void Should_have_error_when_provider_invalid(string provider)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: provider,
                ProviderUserId: "user-123",
                ProviderEmail: "test@example.com"
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Provider)
                .WithErrorCode("users:auth:provider:invalid");
        }

        [Theory]
        [InlineData("google")]
        [InlineData("github")]
        [InlineData("gitlab")]
        public void Should_not_have_error_for_valid_providers(string provider)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: provider,
                ProviderUserId: "user-123",
                ProviderEmail: "test@example.com"
            );

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Provider);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_have_error_when_provider_user_id_empty(string providerUserId)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: providerUserId,
                ProviderEmail: "test@example.com"
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ProviderUserId)
                .WithErrorCode("users:auth:provider_user_id:required");
        }

        [Fact]
        public void Should_have_error_when_provider_user_id_too_long()
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: new string('a', 256),
                ProviderEmail: "test@example.com"
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ProviderUserId)
                .WithErrorCode("users:auth:provider_user_id:too_long");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_have_error_when_provider_email_empty(string providerEmail)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: "user-123",
                ProviderEmail: providerEmail
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ProviderEmail)
                .WithErrorCode("users:auth:provider_email:required");
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("missing-at-sign.com")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        public void Should_have_error_when_provider_email_invalid_format(string providerEmail)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: "user-123",
                ProviderEmail: providerEmail
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ProviderEmail)
                .WithErrorCode("users:auth:provider_email:invalid_format");
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("user+tag@example.co.uk")]
        [InlineData("first.last@subdomain.example.com")]
        public void Should_not_have_error_for_valid_emails(string providerEmail)
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: "user-123",
                ProviderEmail: providerEmail
            );

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.ProviderEmail);
        }

        [Fact]
        public void Should_have_error_when_provider_email_too_long()
        {
            var request = new HandleOAuthCallback.Request(
                Provider: "google",
                ProviderUserId: "user-123",
                ProviderEmail: new string('a', 244) + "@example.com"
            );

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ProviderEmail)
                .WithErrorCode("users:auth:provider_email:too_long");
        }
    }
}
