using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");
        builder.HasKey(resource => resource.UserId);
        builder.Property(resource => resource.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(resource => resource.UtilisationPercent)
            .IsRequired()
            .HasDefaultValue(0);
        builder.HasIndex(resource => resource.ManagerUserId);
        builder.HasOne(resource => resource.User)
            .WithOne(user => user.Resource)
            .HasForeignKey<Resource>(resource => resource.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(resource => resource.Manager)
            .WithMany()
            .HasForeignKey(resource => resource.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
