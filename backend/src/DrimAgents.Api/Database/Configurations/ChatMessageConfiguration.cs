using DrimAgents.Api.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrimAgents.Api.Database.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", "chat");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.ChatSessionId, x.CreatedAt })
            .HasDatabaseName("IX_chat_messages_chat_session_id_created_at");

        builder.HasIndex(x => new { x.ChatSessionId, x.Id })
            .HasDatabaseName("IX_chat_messages_chat_session_id_id");
    }
}
