using Sqordia.Domain.Common;
using Sqordia.Domain.Enums;

namespace Sqordia.Domain.Entities;

/// <summary>
/// Application settings entity for storing key-value configuration
/// </summary>
public class Settings : BaseAuditableEntity
{
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string Category { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public SettingType SettingType { get; private set; } = SettingType.Config;
    public SettingDataType DataType { get; private set; } = SettingDataType.String;
    public bool IsEncrypted { get; private set; } = false;
    public int? CacheDurationMinutes { get; private set; }
    public bool IsCritical { get; private set; } = false;

    private Settings() { } // EF Core constructor

    public Settings(
        string key, 
        string value, 
        string category, 
        string? description = null, 
        bool isPublic = false,
        SettingType settingType = SettingType.Config,
        SettingDataType dataType = SettingDataType.String,
        bool isEncrypted = false,
        int? cacheDurationMinutes = null,
        bool isCritical = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty", nameof(value));

        Key = key;
        Value = value;
        Category = category ?? string.Empty;
        Description = description;
        IsPublic = isPublic;
        SettingType = settingType;
        DataType = dataType;
        IsEncrypted = isEncrypted;
        CacheDurationMinutes = cacheDurationMinutes;
        IsCritical = isCritical;
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty", nameof(value));

        Value = value;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void MarkAsPublic()
    {
        IsPublic = true;
    }

    public void MarkAsPrivate()
    {
        IsPublic = false;
    }

    public void UpdateCategory(string category)
    {
        Category = category ?? string.Empty;
    }

    /// <summary>
    /// Check if this is a feature flag and if it's enabled
    /// </summary>
    public bool IsFeatureEnabled()
    {
        if (SettingType != SettingType.FeatureFlag)
            return false;

        return bool.TryParse(Value, out var result) && result;
    }

    /// <summary>
    /// Get typed value from setting
    /// </summary>
    public T? GetTypedValue<T>()
    {
        if (string.IsNullOrWhiteSpace(Value))
            return default;

        try
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)bool.Parse(Value);
            }
            if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(Value);
            }
            if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Parse(Value);
            }
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Parse(Value);
            }
            if (typeof(T) == typeof(decimal))
            {
                return (T)(object)decimal.Parse(Value);
            }
            if (typeof(T) == typeof(string))
            {
                return (T)(object)Value;
            }

            // Try JSON deserialization for complex types
            return System.Text.Json.JsonSerializer.Deserialize<T>(Value);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Mark setting as critical (load at startup)
    /// </summary>
    public void MarkAsCritical()
    {
        IsCritical = true;
    }

    /// <summary>
    /// Mark setting as non-critical
    /// </summary>
    public void UnmarkAsCritical()
    {
        IsCritical = false;
    }

    /// <summary>
    /// Update setting type
    /// </summary>
    public void UpdateSettingType(SettingType settingType)
    {
        SettingType = settingType;
    }

    /// <summary>
    /// Update data type
    /// </summary>
    public void UpdateDataType(SettingDataType dataType)
    {
        DataType = dataType;
    }

    /// <summary>
    /// Set encryption flag
    /// </summary>
    public void SetEncrypted(bool encrypted)
    {
        IsEncrypted = encrypted;
    }

    /// <summary>
    /// Set cache duration
    /// </summary>
    public void SetCacheDuration(int? minutes)
    {
        CacheDurationMinutes = minutes;
    }
}

