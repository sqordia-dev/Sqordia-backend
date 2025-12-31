using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqordia.Application.Common.Interfaces;
using Sqordia.Application.Common.Models;
using Sqordia.Domain.Entities;
using Sqordia.Domain.Enums;

namespace Sqordia.Application.Services.Implementations;

/// <summary>
/// Service implementation for managing application settings
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISettingsCacheService _cacheService;
    private readonly ISettingsEncryptionService _encryptionService;

    public SettingsService(
        IApplicationDbContext context,
        ILogger<SettingsService> logger,
        ICurrentUserService currentUserService,
        ISettingsCacheService cacheService,
        ISettingsEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _encryptionService = encryptionService;
    }

    public async Task<Result<string>> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cachedValue = _cacheService.Get(key);
            if (cachedValue != null)
            {
                return Result.Success(cachedValue);
            }

            // Load from database
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

            if (setting == null)
            {
                _logger.LogWarning("Setting with key '{Key}' not found", key);
                return Result.Failure<string>(Error.NotFound("Settings.NotFound", $"Setting with key '{key}' not found"));
            }

            // Decrypt if needed
            var value = setting.IsEncrypted && _encryptionService.IsEncryptionAvailable()
                ? _encryptionService.Decrypt(setting.Value)
                : setting.Value;

            // Cache the value
            _cacheService.Set(key, value, setting.CacheDurationMinutes);

            return Result.Success(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving setting with key '{Key}'", key);
            return Result.Failure<string>(Error.Failure("Settings.Error", "An error occurred while retrieving the setting"));
        }
    }

    public async Task<Result<T>> GetTypedSettingAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetSettingAsync(key, cancellationToken);
            if (!result.IsSuccess)
            {
                return Result.Failure<T>(result.Error!);
            }

            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

            if (setting == null)
            {
                return Result.Failure<T>(Error.NotFound("Settings.NotFound", $"Setting with key '{key}' not found"));
            }

            var typedValue = setting.GetTypedValue<T>();
            if (typedValue == null && default(T) != null)
            {
                return Result.Failure<T>(Error.Failure("Settings.ConversionError", $"Failed to convert setting '{key}' to type {typeof(T).Name}"));
            }

            return Result.Success(typedValue!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving typed setting with key '{Key}'", key);
            return Result.Failure<T>(Error.Failure("Settings.Error", "An error occurred while retrieving the setting"));
        }
    }

    public async Task<Result<bool>> IsFeatureEnabledAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetTypedSettingAsync<bool>(featureKey, cancellationToken);
            if (!result.IsSuccess)
            {
                // If feature flag doesn't exist, default to false
                return Result.Success(false);
            }

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature flag '{FeatureKey}'", featureKey);
            return Result.Success(false); // Default to disabled on error
        }
    }

    public async Task<Result<Dictionary<string, string>>> GetPublicSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.Settings
                .Where(s => s.IsPublic && !s.IsDeleted)
                .Select(s => new { s.Key, s.Value })
                .ToListAsync(cancellationToken);

            var result = settings.ToDictionary(s => s.Key, s => s.Value);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public settings");
            return Result.Failure<Dictionary<string, string>>(Error.Failure("Settings.Error", "An error occurred while retrieving public settings"));
        }
    }

    public async Task<Result<Dictionary<string, string>>> GetSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.Settings
                .Where(s => s.Category == category && !s.IsDeleted)
                .Select(s => new { s.Key, s.Value })
                .ToListAsync(cancellationToken);

            var result = settings.ToDictionary(s => s.Key, s => s.Value);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settings for category '{Category}'", category);
            return Result.Failure<Dictionary<string, string>>(Error.Failure("Settings.Error", "An error occurred while retrieving settings"));
        }
    }

    public async Task<Result<Dictionary<string, object>>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Result.Failure<Dictionary<string, object>>(Error.Unauthorized("General.Unauthorized", "Unauthorized"));
            }

            // Check if user is admin (you may need to adjust this based on your role system)
            var isAdmin = await IsAdminAsync(userId.Value, cancellationToken);
            if (!isAdmin)
            {
                return Result.Failure<Dictionary<string, object>>(Error.Forbidden("Settings.Forbidden", "Only administrators can view all settings"));
            }

            var settings = await _context.Settings
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    s.Key,
                    s.Value,
                    s.Category,
                    s.Description,
                    s.IsPublic
                })
                .ToListAsync(cancellationToken);

            var result = settings.ToDictionary(
                s => s.Key,
                s => (object)new
                {
                    Value = s.Value,
                    Category = s.Category,
                    Description = s.Description,
                    IsPublic = s.IsPublic
                });

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all settings");
            return Result.Failure<Dictionary<string, object>>(Error.Failure("Settings.Error", "An error occurred while retrieving settings"));
        }
    }

    public async Task<Result> UpsertSettingAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Result.Failure(Error.Unauthorized("General.Unauthorized", "Unauthorized"));
            }

            var isAdmin = await IsAdminAsync(userId.Value, cancellationToken);
            if (!isAdmin)
            {
                return Result.Failure(Error.Forbidden("Settings.Forbidden", "Only administrators can manage settings"));
            }

            // Encrypt value if requested
            var finalValue = encrypt && _encryptionService.IsEncryptionAvailable()
                ? _encryptionService.Encrypt(value)
                : value;

            var existingSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

            if (existingSetting != null)
            {
                existingSetting.UpdateValue(finalValue);
                existingSetting.UpdateCategory(category);
                existingSetting.UpdateDescription(description);
                existingSetting.UpdateSettingType(settingType);
                existingSetting.UpdateDataType(dataType);
                existingSetting.SetEncrypted(encrypt);
                existingSetting.SetCacheDuration(cacheDurationMinutes);
                if (isPublic)
                    existingSetting.MarkAsPublic();
                else
                    existingSetting.MarkAsPrivate();
                if (isCritical)
                    existingSetting.MarkAsCritical();
                else
                    existingSetting.UnmarkAsCritical();
            }
            else
            {
                var newSetting = new Settings(
                    key, 
                    finalValue, 
                    category, 
                    description, 
                    isPublic,
                    settingType,
                    dataType,
                    encrypt,
                    cacheDurationMinutes,
                    isCritical);
                _context.Settings.Add(newSetting);
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidate cache
            _cacheService.Remove(key);
            
            _logger.LogInformation("Setting '{Key}' upserted successfully", key);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting setting '{Key}'", key);
            return Result.Failure(Error.Failure("Settings.Error", "An error occurred while saving the setting"));
        }
    }

    public async Task<Result> EncryptAndStoreSecretAsync(string key, string secret, string category = "Secrets", string? description = null, CancellationToken cancellationToken = default)
    {
        return await UpsertSettingAsync(
            key,
            secret,
            category,
            description,
            isPublic: false,
            settingType: SettingType.Secret,
            dataType: SettingDataType.String,
            encrypt: true,
            cacheDurationMinutes: null,
            isCritical: false,
            cancellationToken);
    }

    public async Task<Result> UpdateSettingValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Result.Failure(Error.Unauthorized("General.Unauthorized", "Unauthorized"));
            }

            var isAdmin = await IsAdminAsync(userId.Value, cancellationToken);
            if (!isAdmin)
            {
                return Result.Failure(Error.Forbidden("Settings.Forbidden", "Only administrators can update settings"));
            }

            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

            if (setting == null)
            {
                return Result.Failure(Error.NotFound("Settings.NotFound", $"Setting with key '{key}' not found"));
            }

            setting.UpdateValue(value);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            _cacheService.Remove(key);

            _logger.LogInformation("Setting '{Key}' updated successfully", key);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setting '{Key}'", key);
            return Result.Failure(Error.Failure("Settings.Error", "An error occurred while updating the setting"));
        }
    }

    public async Task<Result> DeleteSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Result.Failure(Error.Unauthorized("General.Unauthorized", "Unauthorized"));
            }

            var isAdmin = await IsAdminAsync(userId.Value, cancellationToken);
            if (!isAdmin)
            {
                return Result.Failure(Error.Forbidden("Settings.Forbidden", "Only administrators can delete settings"));
            }

            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

            if (setting == null)
            {
                return Result.Failure(Error.NotFound("Settings.NotFound", $"Setting with key '{key}' not found"));
            }

            setting.SoftDelete();
            await _context.SaveChangesAsync(cancellationToken);

            // Remove from cache
            _cacheService.Remove(key);

            _logger.LogInformation("Setting '{Key}' deleted successfully", key);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting '{Key}'", key);
            return Result.Failure(Error.Failure("Settings.Error", "An error occurred while deleting the setting"));
        }
    }

    public async Task<Result> LoadCriticalSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var criticalSettings = await _context.Settings
                .Where(s => s.IsCritical && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var setting in criticalSettings)
            {
                var value = setting.IsEncrypted && _encryptionService.IsEncryptionAvailable()
                    ? _encryptionService.Decrypt(setting.Value)
                    : setting.Value;

                _cacheService.Set(setting.Key, value, setting.CacheDurationMinutes);
            }

            _logger.LogInformation("Loaded {Count} critical settings into cache", criticalSettings.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading critical settings");
            return Result.Failure(Error.Failure("Settings.Error", "An error occurred while loading critical settings"));
        }
    }

    public async Task<Result> RefreshCacheAsync(string? key = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (key != null)
            {
                // Refresh specific key
                _cacheService.Remove(key);
                var result = await GetSettingAsync(key, cancellationToken);
                return result.IsSuccess 
                    ? Result.Success() 
                    : Result.Failure(Error.NotFound("Settings.NotFound", $"Setting with key '{key}' not found"));
            }
            else
            {
                // Reload all critical settings
                return await LoadCriticalSettingsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache for key '{Key}'", key ?? "all");
            return Result.Failure(Error.Failure("Settings.Error", "An error occurred while refreshing cache"));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdString = _currentUserService.GetUserId();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }
        return userId;
    }

    private async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Check if user has admin role
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);

        return userRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                                  r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
    }
}

