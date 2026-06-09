using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("Allocations");

        builder.HasKey(allocation => allocation.Id);

        builder.Property(allocation => allocation.UtilisationPercent)
            .IsRequired();

        builder.HasOne(allocation => allocation.Employee)
            .WithMany(employee => employee.Allocations)
            .HasForeignKey(allocation => allocation.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(allocation => allocation.Project)
            .WithMany(project => project.Allocations)
            .HasForeignKey(allocation => allocation.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
