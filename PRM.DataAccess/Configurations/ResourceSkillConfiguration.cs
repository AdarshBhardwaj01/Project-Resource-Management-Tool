using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class ResourceSkillConfiguration : IEntityTypeConfiguration<ResourceSkill>
{
    public void Configure(EntityTypeBuilder<ResourceSkill> builder)
    {
        builder.ToTable("ResourceSkills");
        builder.HasKey(resourceSkill => resourceSkill.Id);
        builder.Property(resourceSkill => resourceSkill.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(resourceSkill => resourceSkill.ProficiencyLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.HasIndex(resourceSkill => new { resourceSkill.UserId, resourceSkill.SkillId })
            .IsUnique();
        builder.HasOne(resourceSkill => resourceSkill.Resource)
            .WithMany(resource => resource.Skills)
            .HasForeignKey(resourceSkill => resourceSkill.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(resourceSkill => resourceSkill.Skill)
            .WithMany(skill => skill.ResourceSkills)
            .HasForeignKey(resourceSkill => resourceSkill.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
