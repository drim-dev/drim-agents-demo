using DrimAgents.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrimAgents.Api.Database.Configurations;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts", "users");

        builder.HasKey(o => new { o.Provider, o.ProviderUserId });

        builder.Property(o => o.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ProviderUserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.ProviderEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(o => o.LinkedAt)
            .IsRequired();

        builder.HasIndex(o => o.UserId);

        builder.HasOne(o => o.User)
            .WithMany(u => u.OAuthAccounts)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
