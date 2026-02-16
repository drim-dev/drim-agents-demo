using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using DrimAgents.Api.Common.Exceptions;

namespace DrimAgents.Api.Common.Services;

public partial class GitHubService : IGitHubService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GitHubRepoInfo> ValidateRepositoryAccess(string repoUrl, string pat, CancellationToken ct = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repoUrl);

        var client = _httpClientFactory.CreateClient("GitHub");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);

        var response = await client.GetAsync($"/repos/{owner}/{repo}", ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["gitHubPat"] = ["projects:project:github_pat:invalid"]
            });
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["gitHubRepoUrl"] = ["projects:project:github_repo:access_denied"]
            });
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["gitHubRepoUrl"] = ["projects:project:github_repo:not_found"]
            });
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var fullName = json.GetProperty("full_name").GetString()!;
        var hasPushAccess = json.GetProperty("permissions").GetProperty("push").GetBoolean();

        return new GitHubRepoInfo(fullName, hasPushAccess);
    }

    private static (string Owner, string Repo) ParseOwnerAndRepo(string repoUrl)
    {
        var match = GitHubUrlRegex().Match(repoUrl);
        if (!match.Success)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["gitHubRepoUrl"] = ["projects:project:github_repo_url:invalid_format"]
            });
        }

        return (match.Groups["owner"].Value, match.Groups["repo"].Value);
    }

    [GeneratedRegex(@"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/.]+?)(?:\.git)?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubUrlRegex();
}
