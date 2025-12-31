namespace Sqordia.Domain.Enums;

/// <summary>
/// Data type of the setting value
/// </summary>
public enum SettingDataType
{
    /// <summary>
    /// Boolean value (true/false)
    /// </summary>
    Boolean = 0,

    /// <summary>
    /// String value
    /// </summary>
    String = 1,

    /// <summary>
    /// Numeric value (integer or decimal)
    /// </summary>
    Number = 2,

    /// <summary>
    /// JSON structured data
    /// </summary>
    Json = 3
}

