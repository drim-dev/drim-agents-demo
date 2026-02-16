using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Features.Projects;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Tests.Features.Projects;

[Collection(ProjectsTestsCollection.Name)]
public class UpdateProjectTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public UpdateProjectTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_update_name_and_description()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var encryption = _fixture.Factory.Services.GetRequiredService<DrimAgents.Api.Common.Services.IDataProtectionEncryption>();
        var idFactory = _fixture.Factory.Services.GetRequiredService<IIdFactory>();

        var project = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user.Id,
            Name = "Original Name",
            Description = "Original Description",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = encryption.Encrypt("ghp_testtoken1234"),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        await _fixture.Database.Save(project);

        var projectId = Base32Encoder.Encode(project.Id);
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "Updated Name",
            description = "Updated Description",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = (string?)null
        };

        var response = await client.PutAsJsonAsync($"/api/projects/{projectId}", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.MaskedGitHubPat.Should().Be("····1234");
    }

    [Fact]
    public async Task Should_update_pat_when_provided()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var encryption = _fixture.Factory.Services.GetRequiredService<DrimAgents.Api.Common.Services.IDataProtectionEncryption>();
        var idFactory = _fixture.Factory.Services.GetRequiredService<IIdFactory>();

        var project = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user.Id,
            Name = "My Project",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = encryption.Encrypt("ghp_oldtoken1234"),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        await _fixture.Database.Save(project);

        _fixture.HttpServer.ForClient("GitHub")
            .RespondTo(HttpMethod.Get, "/repos/owner/repo")
            .WithJson(new { full_name = "owner/repo", permissions = new { push = true } });

        var projectId = Base32Encoder.Encode(project.Id);
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "My Project",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = "ghp_newtoken5678"
        };

        var response = await client.PutAsJsonAsync($"/api/projects/{projectId}", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        result!.MaskedGitHubPat.Should().Be("····5678");

        var updatedProject = await _fixture.Database.Execute(async db =>
            await db.Projects.FirstOrDefaultAsync(p => p.Id == project.Id, CreateCancellationToken()));

        updatedProject.Should().NotBeNull();
        var decryptedPat = encryption.Decrypt(updatedProject!.EncryptedGitHubPat);
        decryptedPat.Should().Be("ghp_newtoken5678");
    }

    [Fact]
    public async Task Should_not_change_pat_when_null()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var encryption = _fixture.Factory.Services.GetRequiredService<DrimAgents.Api.Common.Services.IDataProtectionEncryption>();
        var idFactory = _fixture.Factory.Services.GetRequiredService<IIdFactory>();

        var originalEncryptedPat = encryption.Encrypt("ghp_originaltoken");
        var project = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user.Id,
            Name = "My Project",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = originalEncryptedPat,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        await _fixture.Database.Save(project);

        var projectId = Base32Encoder.Encode(project.Id);
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "Updated Name",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = (string?)null
        };

        var response = await client.PutAsJsonAsync($"/api/projects/{projectId}", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedProject = await _fixture.Database.Execute(async db =>
            await db.Projects.FirstOrDefaultAsync(p => p.Id == project.Id, CreateCancellationToken()));

        updatedProject!.EncryptedGitHubPat.Should().Be(originalEncryptedPat);
    }

    [Fact]
    public async Task Should_validate_new_url_with_existing_pat_when_url_changes()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var encryption = _fixture.Factory.Services.GetRequiredService<DrimAgents.Api.Common.Services.IDataProtectionEncryption>();
        var idFactory = _fixture.Factory.Services.GetRequiredService<IIdFactory>();

        var project = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user.Id,
            Name = "My Project",
            GitHubRepoUrl = "https://github.com/owner/old-repo",
            EncryptedGitHubPat = encryption.Encrypt("ghp_existingtoken"),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        await _fixture.Database.Save(project);

        _fixture.HttpServer.ForClient("GitHub")
            .RespondTo(HttpMethod.Get, "/repos/owner/new-repo")
            .WithJson(new { full_name = "owner/new-repo", permissions = new { push = true } });

        var projectId = Base32Encoder.Encode(project.Id);
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "My Project",
            gitHubRepoUrl = "https://github.com/owner/new-repo",
            gitHubPat = (string?)null
        };

        var response = await client.PutAsJsonAsync($"/api/projects/{projectId}", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        result!.GitHubRepoUrl.Should().Be("https://github.com/owner/new-repo");

        _fixture.HttpServer.WasRequested("GitHub", HttpMethod.Get, "/repos/owner/new-repo").Should().BeTrue();
    }

    [Fact]
    public async Task Should_return_not_found_for_other_users_project()
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

        var encryption = _fixture.Factory.Services.GetRequiredService<DrimAgents.Api.Common.Services.IDataProtectionEncryption>();
        var idFactory = _fixture.Factory.Services.GetRequiredService<IIdFactory>();

        var project = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user1.Id,
            Name = "User1 Project",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = encryption.Encrypt("ghp_token1234"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(project);

        var projectId = Base32Encoder.Encode(project.Id);
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user2.Id, user2.Email);
        var body = new
        {
            name = "Hacked",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = (string?)null
        };

        var response = await client.PutAsJsonAsync($"/api/projects/{projectId}", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var client = _fixture.HttpClient.CreateClient();
        var body = new
        {
            name = "My Project",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = (string?)null
        };

        var response = await client.PutAsJsonAsync("/api/projects/someprojectid", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record ProjectResponse(
        string Id,
        string Name,
        string? Description,
        string GitHubRepoUrl,
        string MaskedGitHubPat,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public class ValidatorTests
    {
        private readonly UpdateProject.RequestValidator _validator = new();

        [Fact]
        public void Should_have_error_when_name_is_empty()
        {
            var request = new UpdateProject.Request(1, 1, "", null, "https://github.com/owner/repo", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("projects:project:name:required");
        }

        [Fact]
        public void Should_have_error_when_name_is_too_short()
        {
            var request = new UpdateProject.Request(1, 1, "ab", null, "https://github.com/owner/repo", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("projects:project:name:too_short");
        }

        [Fact]
        public void Should_have_error_when_name_is_too_long()
        {
            var request = new UpdateProject.Request(1, 1, new string('a', 201), null, "https://github.com/owner/repo", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("projects:project:name:too_long");
        }

        [Theory]
        [InlineData("https://gitlab.com/owner/repo")]
        [InlineData("not-a-url")]
        public void Should_have_error_when_github_repo_url_is_invalid(string url)
        {
            var request = new UpdateProject.Request(1, 1, "Name", null, url, null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.GitHubRepoUrl)
                .WithErrorCode("projects:project:github_repo_url:invalid_format");
        }

        [Fact]
        public void Should_not_have_error_when_github_pat_is_null()
        {
            var request = new UpdateProject.Request(1, 1, "Valid Name", null, "https://github.com/owner/repo", null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.GitHubPat);
        }

        [Fact]
        public void Should_have_error_when_github_pat_is_empty_string()
        {
            var request = new UpdateProject.Request(1, 1, "Valid Name", null, "https://github.com/owner/repo", "");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.GitHubPat)
                .WithErrorCode("projects:project:github_pat:empty");
        }

        [Theory]
        [InlineData("https://github.com/owner/repo")]
        [InlineData("https://github.com/owner/repo.git")]
        public void Should_not_have_error_when_github_repo_url_is_valid(string url)
        {
            var request = new UpdateProject.Request(1, 1, "Valid Name", null, url, null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.GitHubRepoUrl);
        }
    }
}
