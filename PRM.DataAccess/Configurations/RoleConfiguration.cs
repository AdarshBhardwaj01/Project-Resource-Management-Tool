using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.RoleName)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(role => role.Description)
            .IsRequired()
            .HasMaxLength(200);
        builder.HasIndex(role => role.RoleName)
            .IsUnique();
    }
}
