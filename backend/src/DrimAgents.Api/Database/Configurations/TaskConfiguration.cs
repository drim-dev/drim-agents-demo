using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrimAgents.Api.Database.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Domain.Tasks.Task>
{
    public void Configure(EntityTypeBuilder<Domain.Tasks.Task> builder)
    {
        builder.ToTable("tasks", "tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Stage)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasOne<Domain.Projects.Project>()
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.ProjectId)
            .HasDatabaseName("IX_tasks_project_id");
    }
}
