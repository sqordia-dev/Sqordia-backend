using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sqordia.Application.Services;
using Sqordia.Persistence.Contexts;
using WebAPI.Middleware;
using System.Text.Json;

namespace WebAPI.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        try
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogWarning("No connection string configured. Skipping database setup.");
                return;
            }

            var db = services.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("Applying database migrations (will create database if it doesn't exist)...");
            
            // MigrateAsync will create the database if it doesn't exist
            await db.Database.MigrateAsync();
            
            logger.LogInformation("Database migrations completed successfully.");

            // Load critical settings after migrations
            await LoadCriticalSettingsAsync(services, logger);
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "3D000" || pgEx.SqlState == "28P01")
        {
            // PostgreSQL error codes:
            // 3D000 = database does not exist
            // 28P01 = authentication failed
            logger.LogWarning(pgEx, "PostgreSQL database connection failed. Error: {Message}", pgEx.Message);
            logger.LogInformation("For PostgreSQL, the database should be created manually or via migrations. Railway creates databases automatically.");
        }
        catch (PostgresException pgEx)
        {
            // PostgreSQL-specific errors
            logger.LogError(pgEx, "PostgreSQL error occurred while applying database migrations. Error Code: {SqlState}, Message: {Message}", pgEx.SqlState, pgEx.Message);
            logger.LogInformation("Application will continue without database connectivity. Please check your DATABASE_URL environment variable.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations. Application will continue without database connectivity.");
        }
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        var environment = app.Environment;

        // Configure Swagger for development
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sqordia API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // HTTPS redirection (can be disabled in Docker if needed)
        if (!environment.IsDevelopment() || Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT") != "true")
        {
            app.UseHttpsRedirection();
        }

        // Custom middleware - Error handling should be first
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<PerformanceMiddleware>();

        // Localization middleware - Must be before MVC
        app.UseRequestLocalization();

        // Rate limiting middleware
        app.UseIpRateLimiting();

        // CORS
        app.UseCors("AllowAll");

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        // Health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description
                    })
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        return app;
    }

    private static async Task LoadCriticalSettingsAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            var settingsService = services.GetService<ISettingsService>();
            if (settingsService == null)
            {
                logger.LogWarning("ISettingsService not available. Skipping critical settings loading.");
                return;
            }

            logger.LogInformation("Loading critical settings into cache...");
            var result = await settingsService.LoadCriticalSettingsAsync();
            
            if (result.IsSuccess)
            {
                logger.LogInformation("Critical settings loaded successfully.");
            }
            else
            {
                logger.LogWarning("Failed to load critical settings: {Error}", result.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading critical settings. Application will continue.");
        }
    }
}

