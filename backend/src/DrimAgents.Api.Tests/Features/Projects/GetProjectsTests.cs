using System.Net;
using System.Net.Http.Json;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DrimAgents.Api.Tests.Features.Projects;

[Collection(ProjectsTestsCollection.Name)]
public class GetProjectsTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public GetProjectsTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_return_only_current_user_projects()
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

        var project1 = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user1.Id,
            Name = "User1 Project",
            GitHubRepoUrl = "https://github.com/owner/repo1",
            EncryptedGitHubPat = encryption.Encrypt("ghp_token1234"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var project2 = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user2.Id,
            Name = "User2 Project",
            GitHubRepoUrl = "https://github.com/owner/repo2",
            EncryptedGitHubPat = encryption.Encrypt("ghp_token5678"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(project1, project2);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user1.Id, user1.Email);

        var response = await client.GetAsync("/api/projects", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("User1 Project");
    }

    [Fact]
    public async Task Should_return_empty_list_when_no_projects()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);

        var response = await client.GetAsync("/api/projects", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_return_projects_ordered_by_created_at_desc()
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

        var olderProject = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user.Id,
            Name = "Older Project",
            GitHubRepoUrl = "https://github.com/owner/repo1",
            EncryptedGitHubPat = encryption.Encrypt("ghp_token1234"),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var newerProject = new Project
        {
            Id = idFactory.CreateId(),
            UserId = user.Id,
            Name = "Newer Project",
            GitHubRepoUrl = "https://github.com/owner/repo2",
            EncryptedGitHubPat = encryption.Encrypt("ghp_token5678"),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        await _fixture.Database.Save(olderProject, newerProject);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);

        var response = await client.GetAsync("/api/projects", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Newer Project");
        result.Items[1].Name.Should().Be("Older Project");
    }

    [Fact]
    public async Task Should_mask_pat_in_response()
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
            Name = "Test Project",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = encryption.Encrypt("ghp_abcdefgh1234"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(project);

        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);

        var response = await client.GetAsync("/api/projects", CreateCancellationToken());

        var result = await response.Content.ReadFromJsonAsync<ProjectsResponse>();
        result!.Items[0].MaskedGitHubPat.Should().Be("····1234");
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var client = _fixture.HttpClient.CreateClient();

        var response = await client.GetAsync("/api/projects", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record ProjectsResponse(ProjectItem[] Items);

    private record ProjectItem(
        string Id,
        string Name,
        string? Description,
        string GitHubRepoUrl,
        string MaskedGitHubPat,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
