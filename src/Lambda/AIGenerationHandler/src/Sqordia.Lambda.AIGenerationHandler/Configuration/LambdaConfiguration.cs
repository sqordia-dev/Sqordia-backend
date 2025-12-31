namespace Sqordia.Lambda.AIGenerationHandler.Configuration;

/// <summary>
/// Lambda function configuration settings
/// </summary>
public class LambdaConfiguration
{
    public string RdsEndpoint { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseUsername { get; set; } = string.Empty;
    public string AwsRegion { get; set; } = "ca-central-1";
    public string Environment { get; set; } = "production";
    public string OpenAISecretName { get; set; } = "sqordia/openai-api-key/production";
    public string ClaudeSecretName { get; set; } = "sqordia/claude-api-key/production";
    public string GeminiSecretName { get; set; } = "sqordia/gemini-api-key/production";
    public string DefaultAiProvider { get; set; } = "openai";
}

