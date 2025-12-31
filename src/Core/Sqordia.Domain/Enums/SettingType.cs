namespace Sqordia.Domain.Enums;

/// <summary>
/// Type of setting stored in the database
/// </summary>
public enum SettingType
{
    /// <summary>
    /// Boolean feature flag for enabling/disabling features
    /// </summary>
    FeatureFlag = 0,

    /// <summary>
    /// Regular configuration value (strings, numbers)
    /// </summary>
    Config = 1,

    /// <summary>
    /// Encrypted sensitive data (API keys, tokens)
    /// </summary>
    Secret = 2,

    /// <summary>
    /// Complex structured data (JSON)
    /// </summary>
    Json = 3
}

