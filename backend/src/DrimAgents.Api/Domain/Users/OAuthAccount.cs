namespace DrimAgents.Api.Domain.Users;

public class OAuthAccount
{
    public long UserId { get; set; }
    public required string Provider { get; set; }
    public required string ProviderUserId { get; set; }
    public required string ProviderEmail { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime LinkedAt { get; set; }
    public User User { get; set; } = null!;
}
