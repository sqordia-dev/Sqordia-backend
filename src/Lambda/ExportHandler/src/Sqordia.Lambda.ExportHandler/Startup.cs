using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sqordia.Lambda.ExportHandler.Configuration;
using Sqordia.Lambda.ExportHandler.Services;

namespace Sqordia.Lambda.ExportHandler;

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
            S3BucketName = configuration["S3_BUCKET_NAME"] ?? string.Empty,
            AwsRegion = configuration["AWS_REGION"] ?? "ca-central-1",
            Environment = configuration["ENVIRONMENT"] ?? "production"
        };

        services.AddSingleton(Options.Create(lambdaConfig));

        // AWS Services
        var region = Amazon.RegionEndpoint.GetBySystemName(lambdaConfig.AwsRegion);
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(region));

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Application Services
        services.AddScoped<IExportProcessor, ExportProcessor>();

        return services.BuildServiceProvider();
    }
}

