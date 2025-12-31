namespace Sqordia.Application.Common.Interfaces;

/// <summary>
/// Service for caching setting values in memory
/// </summary>
public interface ISettingsCacheService
{
    /// <summary>
    /// Get a cached setting value
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>Cached value or null if not found</returns>
    string? Get(string key);

    /// <summary>
    /// Set a setting value in cache
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="value">Setting value</param>
    /// <param name="cacheDurationMinutes">Cache duration in minutes (null for default)</param>
    void Set(string key, string value, int? cacheDurationMinutes = null);

    /// <summary>
    /// Remove a setting from cache
    /// </summary>
    /// <param name="key">Setting key to remove</param>
    void Remove(string key);

    /// <summary>
    /// Clear all cached settings
    /// </summary>
    void Clear();

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>True if key exists in cache</returns>
    bool Exists(string key);
}

