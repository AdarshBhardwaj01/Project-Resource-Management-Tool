using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PRM.Models.Entities;



namespace PRM.DataAccess.Configurations;



public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>

{

    public void Configure(EntityTypeBuilder<Employee> builder)

    {

        builder.ToTable("Employees");



        builder.HasKey(employee => employee.Id);



        builder.Property(employee => employee.FullName)

            .IsRequired()

            .HasMaxLength(100);



        builder.Property(employee => employee.Email)

            .IsRequired()

            .HasMaxLength(100);



        builder.Property(employee => employee.Department)

            .IsRequired()

            .HasMaxLength(50);



        builder.Property(employee => employee.Designation)

            .IsRequired()

            .HasMaxLength(50);



        builder.Property(employee => employee.Status)

            .IsRequired()

            .HasConversion<string>()

            .HasMaxLength(20);



        builder.Property(employee => employee.UtilisationPercent)

            .IsRequired()

            .HasDefaultValue(0);



        builder.HasIndex(employee => employee.UserId)

            .IsUnique();



        builder.HasIndex(employee => employee.ManagerId);



        builder.HasOne(employee => employee.Manager)

            .WithMany()

            .HasForeignKey(employee => employee.ManagerId)

            .OnDelete(DeleteBehavior.Restrict);

    }

}


