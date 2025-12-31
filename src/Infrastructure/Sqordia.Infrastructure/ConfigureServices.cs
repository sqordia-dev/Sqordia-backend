using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Amazon.SQS;
using Sqordia.Application.Common.Interfaces;
using Sqordia.Application.Common.Security;
using Sqordia.Application.Services;
using Sqordia.Application.Services.Implementations;
using Sqordia.Infrastructure.Services;
using Sqordia.Infrastructure.Identity;
using Sqordia.Infrastructure.Localization;
using IIdentityService = Sqordia.Application.Common.Interfaces.IIdentityService;
using IJwtTokenService = Sqordia.Application.Common.Interfaces.IJwtTokenService;

namespace Sqordia.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP Context for getting client IP address
        services.AddHttpContextAccessor();

        // Email service (AWS SES via SQS) - Emails are sent to SQS queue and processed by Lambda
        var emailQueueUrl = configuration["EMAIL_QUEUE_URL"] ?? Environment.GetEnvironmentVariable("EMAIL_QUEUE_URL");
        // Use same region as S3 storage (read from environment variable first)
        var awsRegion = (Environment.GetEnvironmentVariable("AwsStorage__Region") 
                       ?? configuration["AwsStorage:Region"] 
                       ?? "ca-central-1").Trim();
        
        // Validate AWS region early
        if (string.IsNullOrWhiteSpace(awsRegion))
        {
            throw new InvalidOperationException("AWS Region is not configured. Set AwsStorage:Region in configuration or AwsStorage__Region environment variable.");
        }
        
        Amazon.RegionEndpoint regionEndpoint;
        try
        {
            regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Invalid AWS region '{awsRegion}'. Valid regions include: us-east-1, ca-central-1, eu-west-1, etc. Received value: '{awsRegion}'", ex);
        }
        
        if (string.IsNullOrWhiteSpace(emailQueueUrl))
        {
            // Email service is optional - if queue URL is not configured, emails will be logged but not sent
            services.AddTransient<IEmailService>(sp =>
                new EmailService(
                    null, // SQS client will be created if queue URL is available
                    emailQueueUrl,
                    awsRegion,
                    sp.GetRequiredService<ILogger<EmailService>>(),
                    sp.GetRequiredService<ILocalizationService>()));
        }
        else
        {
            // Configure AWS SQS client for email queue
            services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(regionEndpoint));
            services.AddTransient<IEmailService>(sp =>
                new EmailService(
                    sp.GetRequiredService<IAmazonSQS>(),
                    emailQueueUrl,
                    awsRegion,
                    sp.GetRequiredService<ILogger<EmailService>>(),
                    sp.GetRequiredService<ILocalizationService>()));
        }

        // Security service - Required for password hashing
        services.AddTransient<ISecurityService, SecurityService>();

        // Identity services - Required for authentication
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IAccountLockoutService, AccountLockoutService>();
        services.AddTransient<ITotpService, TotpService>();

        // Localization service - Required for bilingual support
        services.AddSingleton<ILocalizationService, LocalizationService>();

        // AI service - Required for business plan generation
        // Configure from both appsettings and environment variables
        // Priority: Environment variables > appsettings.json
        services.Configure<OpenAISettings>(configuration.GetSection("AI:OpenAI"));
        
        // Post-configure to allow environment variables to override appsettings
        // This runs AFTER the initial Configure, so env vars will override
        services.PostConfigure<OpenAISettings>(options =>
        {
            // Try environment variables first (highest priority)
            var envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                          ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey")
                          ?? Environment.GetEnvironmentVariable("AI__OpenAI__ApiKey");
            
            // Then try configuration (appsettings.json)
            var configApiKey = configuration["AI:OpenAI:ApiKey"]
                            ?? configuration["OpenAI:ApiKey"];
            
            // Use first non-empty value found
            var apiKey = envApiKey ?? configApiKey;
            
            // Use first non-empty value found
            if (!string.IsNullOrEmpty(apiKey) && apiKey != "TODO: Add OpenAI API key" && !apiKey.Contains("TODO"))
            {
                options.ApiKey = apiKey;
            }
            
            // Same for model
            var envModel = Environment.GetEnvironmentVariable("OPENAI_MODEL")
                        ?? Environment.GetEnvironmentVariable("OpenAI__Model")
                        ?? Environment.GetEnvironmentVariable("AI__OpenAI__Model");
            
            var configModel = configuration["AI:OpenAI:Model"]
                           ?? configuration["OpenAI:Model"];
            
            var model = envModel ?? configModel;
            
            if (!string.IsNullOrEmpty(model))
            {
                options.Model = model;
            }
        });
        
        services.AddSingleton<IAIService, OpenAIService>();

        // Document export service - Required for PDF/Word export
        services.AddTransient<IDocumentExportService, DocumentExportService>();

        // Financial projection service - Required for financial calculations and projections
        services.AddTransient<IFinancialProjectionService, FinancialProjectionService>();

        // Admin dashboard service - Required for admin management and analytics
        services.AddTransient<IAdminDashboardService, AdminDashboardService>();
        
        // Subscription service
        services.AddScoped<Sqordia.Application.Services.ISubscriptionService, SubscriptionService>();

        // AWS S3 Storage service
        // Reuse awsRegion and regionEndpoint variables declared earlier for Email service
        var awsStorageSettings = new AwsStorageSettings
        {
            BucketName = Environment.GetEnvironmentVariable("AwsStorage__BucketName")
                        ?? configuration["AwsStorage:BucketName"] 
                        ?? "sqordia-documents",
            Region = awsRegion
        };
        services.AddSingleton(Options.Create(awsStorageSettings));
        
        // Reuse the validated regionEndpoint from above
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(regionEndpoint));
        services.AddScoped<IStorageService, S3StorageService>();

        // Memory cache for settings caching
        services.AddMemoryCache();

        // Settings encryption service
        services.AddSingleton<ISettingsEncryptionService, SettingsEncryptionService>();

        // Settings cache service
        services.AddScoped<ISettingsCacheService, SettingsCacheService>();

        return services;
    }
}
