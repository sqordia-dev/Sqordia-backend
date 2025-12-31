using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sqordia.Lambda.AIGenerationHandler.Configuration;
using Sqordia.Lambda.AIGenerationHandler.Services;

namespace Sqordia.Lambda.AIGenerationHandler;

/// <summary>
/// Startup class for configuring dependency injection
/// </summary>
public static class Startup
{
    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var lambdaConfig = new LambdaConfiguration
        {
            RdsEndpoint = configuration["RDS_ENDPOINT"] ?? string.Empty,
            DatabaseName = configuration["DATABASE_NAME"] ?? "SqordiaDb",
            DatabaseUsername = configuration["DATABASE_USERNAME"] ?? "sqordia_admin",
            AwsRegion = configuration["AWS_REGION"] ?? "ca-central-1",
            Environment = configuration["ENVIRONMENT"] ?? "production",
            OpenAISecretName = configuration["OPENAI_SECRET_NAME"] ?? "sqordia/openai-api-key/production",
            ClaudeSecretName = configuration["CLAUDE_SECRET_NAME"] ?? "sqordia/claude-api-key/production",
            GeminiSecretName = configuration["GEMINI_SECRET_NAME"] ?? "sqordia/gemini-api-key/production",
            DefaultAiProvider = configuration["DEFAULT_AI_PROVIDER"] ?? "openai"
        };

        services.AddSingleton(Options.Create(lambdaConfig));

        // AWS Services
        var region = Amazon.RegionEndpoint.GetBySystemName(lambdaConfig.AwsRegion);
        services.AddSingleton<IAmazonSecretsManager>(_ =>
            new AmazonSecretsManagerClient(region));

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Application Services
        services.AddScoped<IAIGenerationProcessor, AIGenerationProcessor>();

        return services.BuildServiceProvider();
    }
}

