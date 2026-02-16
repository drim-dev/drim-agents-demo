using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Features.Projects;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Tests.Features.Projects;

[Collection(ProjectsTestsCollection.Name)]
public class CreateProjectTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public CreateProjectTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_create_project_with_valid_data()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        _fixture.HttpServer.ForClient("GitHub")
            .RespondTo(HttpMethod.Get, "/repos/owner/repo")
            .WithJson(new { full_name = "owner/repo", permissions = new { push = true } });

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "My Project",
            description = "A test project",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = "ghp_testtoken1234"
        };

        var response = await client.PostAsJsonAsync("/api/projects", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("My Project");
        result.Description.Should().Be("A test project");
        result.GitHubRepoUrl.Should().Be("https://github.com/owner/repo");
        result.MaskedGitHubPat.Should().Be("····1234");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var project = await _fixture.Database.Execute(async db =>
            await db.Projects.FirstOrDefaultAsync(p => p.UserId == user.Id, CreateCancellationToken()));

        project.Should().NotBeNull();
        project!.Name.Should().Be("My Project");
        project.EncryptedGitHubPat.Should().NotBe("ghp_testtoken1234");
    }

    [Fact]
    public async Task Should_allow_duplicate_project_names()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        _fixture.HttpServer.ForClient("GitHub")
            .RespondTo(HttpMethod.Get, "/repos/owner/repo")
            .WithJson(new { full_name = "owner/repo", permissions = new { push = true } });

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "Same Name",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = "ghp_testtoken1234"
        };

        var response1 = await client.PostAsJsonAsync("/api/projects", body, CreateCancellationToken());
        var response2 = await client.PostAsJsonAsync("/api/projects", body, CreateCancellationToken());

        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Should_return_bad_request_when_repository_not_found()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        _fixture.HttpServer.ForClient("GitHub")
            .RespondTo(HttpMethod.Get, "/repos/owner/repo")
            .WithStatusCode(HttpStatusCode.NotFound);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "My Project",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = "ghp_testtoken1234"
        };

        var response = await client.PostAsJsonAsync("/api/projects", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_return_bad_request_when_pat_is_invalid()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        _fixture.HttpServer.ForClient("GitHub")
            .RespondTo(HttpMethod.Get, "/repos/owner/repo")
            .WithStatusCode(HttpStatusCode.Unauthorized);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);
        var body = new
        {
            name = "My Project",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = "ghp_invalidtoken"
        };

        var response = await client.PostAsJsonAsync("/api/projects", body, CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var client = _fixture.HttpClient.CreateClient();
        var body = new
        {
            name = "My Project",
            gitHubRepoUrl = "https://github.com/owner/repo",
            gitHubPat = "ghp_testtoken1234"
        };

        var response = await client.PostAsJsonAsync("/api/projects", body, CreateCancellationToken());

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
        private readonly CreateProject.RequestValidator _validator = new();

        [Fact]
        public void Should_have_error_when_name_is_empty()
        {
            var request = new CreateProject.Request(1, "", null, "https://github.com/owner/repo", "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("projects:project:name:required");
        }

        [Fact]
        public void Should_have_error_when_name_is_too_short()
        {
            var request = new CreateProject.Request(1, "ab", null, "https://github.com/owner/repo", "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("projects:project:name:too_short");
        }

        [Fact]
        public void Should_have_error_when_name_is_too_long()
        {
            var request = new CreateProject.Request(1, new string('a', 201), null, "https://github.com/owner/repo", "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("projects:project:name:too_long");
        }

        [Fact]
        public void Should_not_have_error_when_name_is_valid()
        {
            var request = new CreateProject.Request(1, "Valid Name", null, "https://github.com/owner/repo", "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_have_error_when_github_repo_url_is_empty()
        {
            var request = new CreateProject.Request(1, "Name", null, "", "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.GitHubRepoUrl);
        }

        [Theory]
        [InlineData("https://gitlab.com/owner/repo")]
        [InlineData("https://bitbucket.org/owner/repo")]
        [InlineData("not-a-url")]
        public void Should_have_error_when_github_repo_url_is_not_github(string url)
        {
            var request = new CreateProject.Request(1, "Name", null, url, "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.GitHubRepoUrl)
                .WithErrorCode("projects:project:github_repo_url:invalid_format");
        }

        [Theory]
        [InlineData("https://github.com/owner")]
        [InlineData("https://github.com/")]
        public void Should_have_error_when_github_repo_url_missing_owner_repo(string url)
        {
            var request = new CreateProject.Request(1, "Name", null, url, "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.GitHubRepoUrl);
        }

        [Theory]
        [InlineData("https://github.com/owner/repo")]
        [InlineData("https://github.com/owner/repo.git")]
        [InlineData("https://github.com/owner/repo/")]
        public void Should_not_have_error_when_github_repo_url_is_valid(string url)
        {
            var request = new CreateProject.Request(1, "Name", null, url, "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.GitHubRepoUrl);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_have_error_when_github_pat_is_empty(string pat)
        {
            var request = new CreateProject.Request(1, "Name", null, "https://github.com/owner/repo", pat);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.GitHubPat)
                .WithErrorCode("projects:project:github_pat:required");
        }

        [Fact]
        public void Should_not_have_error_when_description_is_null()
        {
            var request = new CreateProject.Request(1, "Valid Name", null, "https://github.com/owner/repo", "ghp_token");
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }
    }
}
