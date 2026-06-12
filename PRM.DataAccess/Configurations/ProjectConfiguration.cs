using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(project => project.Description)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(project => project.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(project => project.HealthStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(project => project.AtRiskNotificationSentAt)
            .IsRequired(false);
        builder.HasOne(project => project.Manager)
            .WithMany(user => user.ManagedProjects)
            .HasForeignKey(project => project.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
