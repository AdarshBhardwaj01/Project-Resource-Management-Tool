using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);
        builder.Property(user => user.Department)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);
        builder.Property(user => user.Designation)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);
        builder.HasIndex(user => user.Username)
            .IsUnique();
        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}
