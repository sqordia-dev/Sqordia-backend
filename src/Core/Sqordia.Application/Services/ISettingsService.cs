using Sqordia.Application.Common.Models;
using Sqordia.Domain.Enums;

namespace Sqordia.Application.Services;

/// <summary>
/// Service interface for managing application settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get a setting by key (with caching and decryption)
    /// </summary>
    Task<Result<string>> GetSettingAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a typed setting value
    /// </summary>
    Task<Result<T>> GetTypedSettingAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature flag is enabled
    /// </summary>
    Task<Result<bool>> IsFeatureEnabledAsync(string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all public settings
    /// </summary>
    Task<Result<Dictionary<string, string>>> GetPublicSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get settings by category
    /// </summary>
    Task<Result<Dictionary<string, string>>> GetSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all settings (admin only)
    /// </summary>
    Task<Result<Dictionary<string, object>>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update a setting (admin only)
    /// </summary>
    Task<Result> UpsertSettingAsync(
        string key, 
        string value, 
        string category, 
        string? description = null, 
        bool isPublic = false,
        SettingType settingType = SettingType.Config,
        SettingDataType dataType = SettingDataType.String,
        bool encrypt = false,
        int? cacheDurationMinutes = null,
        bool isCritical = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Store an encrypted secret (admin only)
    /// </summary>
    Task<Result> EncryptAndStoreSecretAsync(string key, string secret, string category = "Secrets", string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update setting value (admin only)
    /// </summary>
    Task<Result> UpdateSettingValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a setting (admin only)
    /// </summary>
    Task<Result> DeleteSettingAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load all critical settings at startup
    /// </summary>
    Task<Result> LoadCriticalSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh cache for a specific key or all settings
    /// </summary>
    Task<Result> RefreshCacheAsync(string? key = null, CancellationToken cancellationToken = default);
}

