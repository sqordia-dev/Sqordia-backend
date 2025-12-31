using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
// CloudWatch sink temporarily disabled due to AWSSDK version conflict
// using Serilog.Sinks.AwsCloudWatch;
// using Amazon.CloudWatchLogs;
// using Amazon;
using Serilog.Enrichers;
using Sqordia.Application;
using Sqordia.Infrastructure;
using Sqordia.Persistence;
using WebAPI.Configuration;
using WebAPI.Extensions;

// Configure Serilog early for bootstrap logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Sqordia application");

    var builder = WebApplication.CreateBuilder(args);
    
    var customConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
    if (!string.IsNullOrEmpty(customConnectionString))
    {
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", customConnectionString }
        });
    }
    
    // Configure Serilog with CloudWatch for production
    var awsRegion = builder.Configuration["AwsStorage:Region"] ?? "ca-central-1";
    var logGroupName = builder.Configuration["Serilog:CloudWatch:LogGroupName"] ?? "/aws/sqordia/api";
    var logStreamName = builder.Configuration["Serilog:CloudWatch:LogStreamName"];
    if (string.IsNullOrEmpty(logStreamName))
    {
        logStreamName = $"{Environment.MachineName}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
    var isProduction = builder.Environment.IsProduction();
    
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Sqordia");

        // CloudWatch sink temporarily disabled due to AWSSDK version conflict
        // Will be re-enabled once we resolve the AWSSDK.Core version conflict
        // For now, logs will go to console and file only
        /*
        var enableCloudWatch = isProduction || 
                              context.Configuration.GetValue<bool>("Serilog:CloudWatch:Enabled", false);
        
        if (enableCloudWatch)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);
            var cloudWatchClient = new AmazonCloudWatchLogsClient(regionEndpoint);
            
            configuration.WriteTo.AmazonCloudWatch(
                new CloudWatchSinkOptions
                {
                    LogGroupName = logGroupName,
                    LogStreamNameProvider = new DefaultLogStreamProvider(),
                    TextFormatter = new JsonFormatter(),
                    MinimumLogEventLevel = LogEventLevel.Information,
                    BatchSizeLimit = 100,
                    Period = TimeSpan.FromSeconds(5),
                    QueueSizeLimit = 10000,
                    RetryAttempts = 3
                },
                cloudWatchClient);
            
            Log.Information("CloudWatch logging enabled. LogGroup: {LogGroup}, Region: {Region}", 
                logGroupName, awsRegion);
        }
        */
    });

    builder.Services.AddApplicationConfiguration(builder.Configuration);

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddAuthenticationServices(builder.Configuration);
    builder.Services.AddCorsServices();
    builder.Services.AddLocalizationServices();
    builder.Services.AddRateLimitingServices(builder.Configuration);
    builder.Services.AddHealthCheckServices();

    var app = builder.Build();

    await app.ApplyDatabaseMigrationsAsync();
    app.ConfigureMiddleware();

    Log.Information("Sqordia application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
