using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.ToTable("Timesheets");
        builder.HasKey(timesheet => timesheet.Id);
        builder.Property(timesheet => timesheet.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.HasIndex(timesheet => new { timesheet.UserId, timesheet.WeekStartDate })
            .IsUnique();
        builder.HasOne(timesheet => timesheet.Resource)
            .WithMany(resource => resource.Timesheets)
            .HasForeignKey(timesheet => timesheet.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
