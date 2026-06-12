using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Models.Entities;

namespace PRM.DataAccess.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("SystemConfigs");
        builder.HasKey(config => config.Id);
        builder.Property(config => config.LlmProvider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(config => config.LlmApiKey)
            .IsRequired()
            .HasMaxLength(500);
    }
}
