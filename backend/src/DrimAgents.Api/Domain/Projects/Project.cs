using DrimAgents.Api.Domain.Users;

namespace DrimAgents.Api.Domain.Projects;

public class Project
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string GitHubRepoUrl { get; set; }
    public required string EncryptedGitHubPat { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
