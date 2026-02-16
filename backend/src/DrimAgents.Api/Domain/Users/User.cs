using DrimAgents.Api.Domain.Projects;

namespace DrimAgents.Api.Domain.Users;

public class User
{
    public long Id { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<OAuthAccount> OAuthAccounts { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
