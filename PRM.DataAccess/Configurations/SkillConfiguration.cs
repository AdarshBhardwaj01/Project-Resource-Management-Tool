using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(skill => skill.Id);
        builder.Property(skill => skill.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(skill => skill.Name)
            .IsUnique();
    }
}
