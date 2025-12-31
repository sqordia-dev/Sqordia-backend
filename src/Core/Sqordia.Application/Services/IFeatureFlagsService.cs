using Sqordia.Application.Common.Models;

namespace Sqordia.Application.Services;

/// <summary>
/// Service interface for managing feature flags
/// </summary>
public interface IFeatureFlagsService
{
    /// <summary>
    /// Check if a feature is enabled
    /// </summary>
    Task<Result<bool>> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable a feature flag
    /// </summary>
    Task<Result> EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disable a feature flag
    /// </summary>
    Task<Result> DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all feature flags
    /// </summary>
    Task<Result<Dictionary<string, bool>>> GetAllFeaturesAsync(CancellationToken cancellationToken = default);
}

