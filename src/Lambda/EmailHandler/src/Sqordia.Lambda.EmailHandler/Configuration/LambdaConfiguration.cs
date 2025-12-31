namespace Sqordia.Lambda.EmailHandler.Configuration;

/// <summary>
/// Lambda function configuration settings
/// </summary>
public class LambdaConfiguration
{
    public string RdsEndpoint { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseUsername { get; set; } = string.Empty;
    public string SesFromEmail { get; set; } = string.Empty;
    public string SesFromName { get; set; } = "Sqordia";
    public string AwsRegion { get; set; } = "ca-central-1";
    public string Environment { get; set; } = "production";
}

