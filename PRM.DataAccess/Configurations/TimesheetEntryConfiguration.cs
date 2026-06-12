using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> builder)
    {
        builder.ToTable("TimesheetEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Hours)
            .HasPrecision(5, 2);
        builder.Property(entry => entry.ActivityTags)
            .IsRequired()
            .HasMaxLength(500);
        builder.HasOne(entry => entry.Timesheet)
            .WithMany(timesheet => timesheet.Entries)
            .HasForeignKey(entry => entry.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entry => entry.Project)
            .WithMany(project => project.TimesheetEntries)
            .HasForeignKey(entry => entry.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
