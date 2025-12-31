using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Sqordia.Lambda.AIGenerationHandler.Configuration;
using Sqordia.Lambda.AIGenerationHandler.Models;
using System.Text.Json;

namespace Sqordia.Lambda.AIGenerationHandler.Services;

/// <summary>
/// Service implementation for processing AI business plan generation jobs
/// </summary>
public class AIGenerationProcessor : IAIGenerationProcessor
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly ILogger<AIGenerationProcessor> _logger;
    private readonly LambdaConfiguration _config;

    public AIGenerationProcessor(
        IAmazonSecretsManager secretsManager,
        ILogger<AIGenerationProcessor> logger,
        IOptions<LambdaConfiguration> config)
    {
        _secretsManager = secretsManager;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<bool> ProcessGenerationJobAsync(AIGenerationJobMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing AI generation job {JobId} for business plan {BusinessPlanId}",
                message.JobId,
                message.BusinessPlanId);

            // Get AI API key from Secrets Manager
            var apiKey = await GetApiKeyAsync(message.AiProvider ?? _config.DefaultAiProvider, cancellationToken);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Failed to retrieve API key for provider {Provider}", message.AiProvider);
                return false;
            }

            // Update business plan status to "Generating"
            await UpdateBusinessPlanStatusAsync(message.BusinessPlanId, "Generating", cancellationToken);

            // Get available sections to generate
            var sectionsToGenerate = message.Sections ?? GetAvailableSections(message.PlanType);
            var totalSections = sectionsToGenerate.Count;
            var completedSections = 0;

            // Generate each section
            foreach (var section in sectionsToGenerate)
            {
                _logger.LogInformation(
                    "Generating section {Section} ({Completed}/{Total}) for business plan {BusinessPlanId}",
                    section,
                    completedSections + 1,
                    totalSections,
                    message.BusinessPlanId);

                var content = await GenerateSectionContentAsync(
                    section,
                    message.QuestionnaireContext ?? new Dictionary<string, object>(),
                    message.Language,
                    apiKey,
                    message.AiProvider ?? _config.DefaultAiProvider,
                    cancellationToken);

                // Update business plan with generated content
                await UpdateBusinessPlanSectionAsync(
                    message.BusinessPlanId,
                    section,
                    content,
                    cancellationToken);

                completedSections++;
            }

            // Mark business plan as completed
            await UpdateBusinessPlanStatusAsync(message.BusinessPlanId, "Completed", cancellationToken);

            _logger.LogInformation(
                "Successfully completed AI generation job {JobId} for business plan {BusinessPlanId}",
                message.JobId,
                message.BusinessPlanId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing AI generation job {JobId} for business plan {BusinessPlanId}",
                message.JobId,
                message.BusinessPlanId);

            // Update status to failed
            try
            {
                await UpdateBusinessPlanStatusAsync(message.BusinessPlanId, "Failed", cancellationToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update business plan status to Failed");
            }

            throw;
        }
    }

    private async Task<string> GetApiKeyAsync(string provider, CancellationToken cancellationToken)
    {
        try
        {
            var secretName = provider.ToLower() switch
            {
                "openai" => _config.OpenAISecretName,
                "claude" => _config.ClaudeSecretName,
                "gemini" => _config.GeminiSecretName,
                _ => _config.OpenAISecretName
            };

            var response = await _secretsManager.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = secretName },
                cancellationToken);

            return response.SecretString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key for provider {Provider}", provider);
            throw;
        }
    }

    private async Task<string> GenerateSectionContentAsync(
        string section,
        Dictionary<string, object> context,
        string language,
        string apiKey,
        string provider,
        CancellationToken cancellationToken)
    {
        // AI generation logic implementation
        // This would call OpenAI, Claude, or Gemini APIs based on provider
        // Note: Full implementation pending - currently returns placeholder content
        
        _logger.LogInformation("Generating content for section {Section} using provider {Provider}", section, provider);
        
        await Task.Delay(1000, cancellationToken); // Simulate AI call
        
        return $"Generated content for {section} section in {language}";
    }

    private async Task UpdateBusinessPlanStatusAsync(
        string businessPlanId,
        string status,
        CancellationToken cancellationToken)
    {
        var connectionString = BuildConnectionString();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE ""BusinessPlans""
            SET ""Status"" = @Status, ""UpdatedAt"" = @UpdatedAt
            WHERE ""Id"" = @Id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Id", Guid.Parse(businessPlanId));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateBusinessPlanSectionAsync(
        string businessPlanId,
        string section,
        string content,
        CancellationToken cancellationToken)
    {
        var connectionString = BuildConnectionString();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        // Map section name to database column
        var columnName = MapSectionToColumn(section);
        if (string.IsNullOrEmpty(columnName))
        {
            _logger.LogWarning("Unknown section name: {Section}", section);
            return;
        }

        var sql = $@"
            UPDATE ""BusinessPlans""
            SET ""{columnName}"" = @Content, ""UpdatedAt"" = @UpdatedAt
            WHERE ""Id"" = @Id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Content", content);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Id", Guid.Parse(businessPlanId));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private string MapSectionToColumn(string section)
    {
        return section.ToLower() switch
        {
            "executive-summary" => "ExecutiveSummary",
            "problem-statement" => "ProblemStatement",
            "solution" => "Solution",
            "market-analysis" => "MarketAnalysis",
            "competitive-analysis" => "CompetitiveAnalysis",
            "financial-projections" => "FinancialProjections",
            "marketing-strategy" => "MarketingStrategy",
            "management-team" => "ManagementTeam",
            _ => string.Empty
        };
    }

    private List<string> GetAvailableSections(string planType)
    {
        return planType.ToLower() switch
        {
            "obnl" => new List<string>
            {
                "executive-summary",
                "problem-statement",
                "solution",
                "market-analysis"
            },
            _ => new List<string>
            {
                "executive-summary",
                "problem-statement",
                "solution",
                "market-analysis",
                "competitive-analysis",
                "financial-projections",
                "marketing-strategy",
                "management-team"
            }
        };
    }

    private string BuildConnectionString()
    {
        // Note: Password should come from Secrets Manager in production
        return $"Host={_config.RdsEndpoint};" +
               $"Database={_config.DatabaseName};" +
               $"Username={_config.DatabaseUsername};" +
               $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? ""};" +
               $"SSL Mode=Require;";
    }
}

