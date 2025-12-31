using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sqordia.Domain.Entities;
using Sqordia.Domain.Enums;

namespace Sqordia.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Settings entity
/// </summary>
public class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(s => s.Id);

        // Key is required and unique
        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(s => s.Key)
            .IsUnique()
            .HasDatabaseName("IX_Settings_Key");

        // Value is required (can be longer for encrypted values)
        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(8000); // Increased for encrypted base64 values

        // Category
        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        // Description is optional
        builder.Property(s => s.Description)
            .HasMaxLength(500);

        // IsPublic
        builder.Property(s => s.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        // SettingType (enum stored as int)
        builder.Property(s => s.SettingType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SettingType.Config);

        // DataType (enum stored as int)
        builder.Property(s => s.DataType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SettingDataType.String);

        // IsEncrypted
        builder.Property(s => s.IsEncrypted)
            .IsRequired()
            .HasDefaultValue(false);

        // CacheDurationMinutes (nullable)
        builder.Property(s => s.CacheDurationMinutes)
            .IsRequired(false);

        // IsCritical
        builder.Property(s => s.IsCritical)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(s => s.Category)
            .HasDatabaseName("IX_Settings_Category");

        builder.HasIndex(s => s.IsPublic)
            .HasDatabaseName("IX_Settings_IsPublic");

        builder.HasIndex(s => s.SettingType)
            .HasDatabaseName("IX_Settings_SettingType");

        builder.HasIndex(s => s.IsCritical)
            .HasDatabaseName("IX_Settings_IsCritical");

        // Composite index for critical settings queries
        builder.HasIndex(s => new { s.IsCritical, s.SettingType })
            .HasDatabaseName("IX_Settings_Critical_Type");
    }
}

