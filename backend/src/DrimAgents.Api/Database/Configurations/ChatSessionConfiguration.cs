using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrimAgents.Api.Database.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions", "chat");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Stage)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.ClaudeSessionId)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Task)
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Messages)
            .WithOne(x => x.ChatSession)
            .HasForeignKey(x => x.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TaskId, x.Stage })
            .IsUnique()
            .HasDatabaseName("IX_chat_sessions_task_id_stage");
    }
}
