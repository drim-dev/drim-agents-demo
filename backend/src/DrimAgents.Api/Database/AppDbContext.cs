using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Projects;
using DrimAgents.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Domain.Tasks.Task> ProjectTasks => Set<Domain.Tasks.Task>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
