using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sqordia.Application.Common.Interfaces;

namespace Sqordia.Infrastructure.Services;

/// <summary>
/// In-memory cache service for settings with TTL support
/// </summary>
public class SettingsCacheService : ISettingsCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SettingsCacheService> _logger;
    private const int DefaultCacheDurationMinutes = 5;

    public SettingsCacheService(IMemoryCache memoryCache, ILogger<SettingsCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public string? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            if (_memoryCache.TryGetValue<string>(GetCacheKey(key), out var value))
            {
                _logger.LogDebug("Cache hit for setting key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache miss for setting key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return null;
        }
    }

    public void Set(string key, string value, int? cacheDurationMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            var duration = cacheDurationMinutes ?? DefaultCacheDurationMinutes;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(duration),
                SlidingExpiration = TimeSpan.FromMinutes(duration / 2) // Refresh on access
            };

            _memoryCache.Set(GetCacheKey(key), value, cacheOptions);
            _logger.LogDebug("Cached setting key: {Key} for {Duration} minutes", key, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            _memoryCache.Remove(GetCacheKey(key));
            _logger.LogDebug("Removed from cache: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache for key: {Key}", key);
        }
    }

    public void Clear()
    {
        // IMemoryCache doesn't have a Clear method, so we track keys or use a wrapper
        // For now, we'll just log - in production, consider using a distributed cache or tracking keys
        _logger.LogWarning("Clear cache called - IMemoryCache doesn't support clearing all entries. Consider using distributed cache or tracking keys.");
    }

    public bool Exists(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _memoryCache.TryGetValue<string>(GetCacheKey(key), out _);
    }

    private static string GetCacheKey(string key)
    {
        return $"Settings:{key}";
    }
}

