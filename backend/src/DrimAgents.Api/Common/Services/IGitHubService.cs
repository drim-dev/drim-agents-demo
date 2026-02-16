namespace DrimAgents.Api.Common.Services;

public interface IGitHubService
{
    Task<GitHubRepoInfo> ValidateRepositoryAccess(string repoUrl, string pat, CancellationToken ct = default);
}
