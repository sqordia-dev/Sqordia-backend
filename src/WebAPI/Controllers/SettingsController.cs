using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqordia.Application.Common.Models;
using Sqordia.Application.Services;
using Sqordia.Domain.Enums;
using WebAPI.Controllers;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for managing application settings
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SettingsController : BaseApiController
{
    private readonly ISettingsService _settingsService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService,
        IFeatureFlagsService featureFlagsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _featureFlagsService = featureFlagsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all public settings
    /// </summary>
    /// <returns>Dictionary of public settings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicSettings(CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetPublicSettingsAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a specific setting by key
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>Setting value</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSetting(string key, CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetSettingAsync(key, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>Dictionary of settings in the category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsByCategory(string category, CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetSettingsByCategoryAsync(category, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all settings (admin only)
    /// </summary>
    /// <returns>Dictionary of all settings with metadata</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllSettings(CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetAllSettingsAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create or update a setting (admin only)
    /// </summary>
    /// <param name="request">Setting data</param>
    /// <returns>Success result</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpsertSetting([FromBody] UpsertSettingRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _settingsService.UpsertSettingAsync(
            request.Key,
            request.Value,
            request.Category,
            request.Description,
            request.IsPublic,
            request.SettingType,
            request.DataType,
            request.Encrypt,
            request.CacheDurationMinutes,
            request.IsCritical,
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Update a setting value (admin only)
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="request">Update request</param>
    /// <returns>Success result</returns>
    [HttpPut("{key}")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _settingsService.UpdateSettingValueAsync(key, request.Value, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a setting (admin only)
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>Success result</returns>
    [HttpDelete("{key}")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSetting(string key, CancellationToken cancellationToken)
    {
        var result = await _settingsService.DeleteSettingAsync(key, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all feature flags
    /// </summary>
    /// <returns>Dictionary of feature flags</returns>
    [HttpGet("features")]
    [ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFeatures(CancellationToken cancellationToken)
    {
        var result = await _featureFlagsService.GetAllFeaturesAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Check if a feature is enabled
    /// </summary>
    /// <param name="featureName">Feature name</param>
    /// <returns>True if enabled, false otherwise</returns>
    [HttpGet("features/{featureName}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsFeatureEnabled(string featureName, CancellationToken cancellationToken)
    {
        var result = await _featureFlagsService.IsEnabledAsync(featureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Enable a feature flag (admin only)
    /// </summary>
    /// <param name="featureName">Feature name</param>
    /// <returns>Success result</returns>
    [HttpPost("features/{featureName}/enable")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EnableFeature(string featureName, CancellationToken cancellationToken)
    {
        var result = await _featureFlagsService.EnableFeatureAsync(featureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Disable a feature flag (admin only)
    /// </summary>
    /// <param name="featureName">Feature name</param>
    /// <returns>Success result</returns>
    [HttpPost("features/{featureName}/disable")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DisableFeature(string featureName, CancellationToken cancellationToken)
    {
        var result = await _featureFlagsService.DisableFeatureAsync(featureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Store an encrypted secret (admin only)
    /// </summary>
    /// <param name="key">Secret key</param>
    /// <param name="request">Secret data</param>
    /// <returns>Success result</returns>
    [HttpPost("secrets/{key}")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StoreSecret(string key, [FromBody] StoreSecretRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _settingsService.EncryptAndStoreSecretAsync(
            key,
            request.Secret,
            request.Category ?? "Secrets",
            request.Description,
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Refresh cache for a setting or all critical settings (admin only)
    /// </summary>
    /// <param name="key">Optional setting key to refresh</param>
    /// <returns>Success result</returns>
    [HttpPost("cache/refresh")]
    [Authorize(Roles = "Admin,Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RefreshCache([FromQuery] string? key, CancellationToken cancellationToken)
    {
        var result = await _settingsService.RefreshCacheAsync(key, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Request model for creating or updating a setting
/// </summary>
public class UpsertSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public SettingType SettingType { get; set; } = SettingType.Config;
    public SettingDataType DataType { get; set; } = SettingDataType.String;
    public bool Encrypt { get; set; } = false;
    public int? CacheDurationMinutes { get; set; }
    public bool IsCritical { get; set; } = false;
}

/// <summary>
/// Request model for updating a setting value
/// </summary>
public class UpdateSettingRequest
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Request model for storing a secret
/// </summary>
public class StoreSecretRequest
{
    public string Secret { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
}

