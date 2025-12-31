using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqordia.Application.Common.Models;
using Sqordia.Application.Services;
using Sqordia.Domain.Enums;

namespace Sqordia.Application.Services.Implementations;

/// <summary>
/// Service for managing feature flags
/// </summary>
public class FeatureFlagsService : IFeatureFlagsService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<FeatureFlagsService> _logger;

    public FeatureFlagsService(ISettingsService settingsService, ILogger<FeatureFlagsService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<Result<bool>> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            return await _settingsService.IsFeatureEnabledAsync(featureKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature flag '{FeatureName}'", featureName);
            return Result.Success(false); // Default to disabled on error
        }
    }

    public async Task<Result> EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            return await _settingsService.UpsertSettingAsync(
                featureKey,
                "true",
                "Features",
                $"Feature flag for {featureName}",
                isPublic: true,
                settingType: SettingType.FeatureFlag,
                dataType: SettingDataType.Boolean,
                encrypt: false,
                cacheDurationMinutes: 5,
                isCritical: false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling feature '{FeatureName}'", featureName);
            return Result.Failure(Error.Failure("FeatureFlags.Error", "An error occurred while enabling the feature"));
        }
    }

    public async Task<Result> DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureKey = GetFeatureKey(featureName);
            return await _settingsService.UpsertSettingAsync(
                featureKey,
                "false",
                "Features",
                $"Feature flag for {featureName}",
                isPublic: true,
                settingType: SettingType.FeatureFlag,
                dataType: SettingDataType.Boolean,
                encrypt: false,
                cacheDurationMinutes: 5,
                isCritical: false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling feature '{FeatureName}'", featureName);
            return Result.Failure(Error.Failure("FeatureFlags.Error", "An error occurred while disabling the feature"));
        }
    }

    public async Task<Result<Dictionary<string, bool>>> GetAllFeaturesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _settingsService.GetSettingsByCategoryAsync("Features", cancellationToken);
            if (!result.IsSuccess)
            {
                return Result.Failure<Dictionary<string, bool>>(result.Error!);
            }

            var features = new Dictionary<string, bool>();
            foreach (var kvp in result.Value)
            {
                var featureName = kvp.Key.Replace("Features:", "", StringComparison.OrdinalIgnoreCase);
                if (bool.TryParse(kvp.Value, out var isEnabled))
                {
                    features[featureName] = isEnabled;
                }
            }

            return Result.Success(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all feature flags");
            return Result.Failure<Dictionary<string, bool>>(Error.Failure("FeatureFlags.Error", "An error occurred while retrieving feature flags"));
        }
    }

    private static string GetFeatureKey(string featureName)
    {
        if (featureName.StartsWith("Features:", StringComparison.OrdinalIgnoreCase))
        {
            return featureName;
        }
        return $"Features:{featureName}";
    }
}

