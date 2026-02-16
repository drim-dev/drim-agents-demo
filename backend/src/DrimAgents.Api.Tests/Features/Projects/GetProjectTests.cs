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
public class GetProjectTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public GetProjectTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Reset(CreateCancellationToken());
    public Task DisposeAsync() => Task.CompletedTask;

    private static CancellationToken CreateCancellationToken() =>
        new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    [Fact]
    public async Task Should_return_own_project()
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
            Description = "Test description",
            GitHubRepoUrl = "https://github.com/owner/repo",
            EncryptedGitHubPat = encryption.Encrypt("ghp_testtoken1234"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(project);

        var projectId = Base32Encoder.Encode(project.Id);
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);

        var response = await client.GetAsync($"/api/projects/{projectId}", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(projectId);
        result.Name.Should().Be("My Project");
        result.Description.Should().Be("Test description");
        result.GitHubRepoUrl.Should().Be("https://github.com/owner/repo");
        result.MaskedGitHubPat.Should().Be("····1234");
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

        var response = await client.GetAsync($"/api/projects/{projectId}", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_not_found_for_nonexistent_project()
    {
        var user = new User
        {
            Email = "test@example.com",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _fixture.Database.Save(user);

        var idFactory = _fixture.Factory.Services.GetRequiredService<IIdFactory>();
        var fakeId = Base32Encoder.Encode(idFactory.CreateId());
        var client = _fixture.HttpClient.CreateAuthenticatedClient(user.Id, user.Email);

        var response = await client.GetAsync($"/api/projects/{fakeId}", CreateCancellationToken());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_unauthorized_when_not_authenticated()
    {
        var client = _fixture.HttpClient.CreateClient();

        var response = await client.GetAsync("/api/projects/someprojectid", CreateCancellationToken());

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
}
