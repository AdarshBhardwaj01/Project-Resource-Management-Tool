using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class EmployeeSkillConfiguration : IEntityTypeConfiguration<EmployeeSkill>
{
    public void Configure(EntityTypeBuilder<EmployeeSkill> builder)
    {
        builder.ToTable("EmployeeSkills");

        builder.HasKey(employeeSkill => employeeSkill.Id);

        builder.Property(employeeSkill => employeeSkill.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(employeeSkill => employeeSkill.ProficiencyLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(employeeSkill => new { employeeSkill.EmployeeId, employeeSkill.SkillId })
            .IsUnique();

        builder.HasOne(employeeSkill => employeeSkill.Employee)
            .WithMany(employee => employee.Skills)
            .HasForeignKey(employeeSkill => employeeSkill.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(employeeSkill => employeeSkill.Skill)
            .WithMany(skill => skill.EmployeeSkills)
            .HasForeignKey(employeeSkill => employeeSkill.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
