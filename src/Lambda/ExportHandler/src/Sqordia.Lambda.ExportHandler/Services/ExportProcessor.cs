using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sqordia.Lambda.ExportHandler.Configuration;
using Sqordia.Lambda.ExportHandler.Models;
using System.Text;

namespace Sqordia.Lambda.ExportHandler.Services;

/// <summary>
/// Service implementation for processing document export jobs
/// </summary>
public class ExportProcessor : IExportProcessor
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<ExportProcessor> _logger;
    private readonly LambdaConfiguration _config;

    public ExportProcessor(
        IAmazonS3 s3Client,
        ILogger<ExportProcessor> logger,
        IOptions<LambdaConfiguration> config)
    {
        _s3Client = s3Client;
        _logger = logger;
        _config = config.Value;

        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<bool> ProcessExportJobAsync(ExportJobMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing export job {JobId} for business plan {BusinessPlanId}, type: {ExportType}",
                message.JobId,
                message.BusinessPlanId,
                message.ExportType);

            // Get business plan data from database
            var businessPlan = await GetBusinessPlanAsync(message.BusinessPlanId, cancellationToken);
            if (businessPlan == null)
            {
                _logger.LogError("Business plan {BusinessPlanId} not found", message.BusinessPlanId);
                return false;
            }

            byte[] documentBytes;
            string fileName;
            string contentType;

            // Generate document based on type
            switch (message.ExportType.ToLower())
            {
                case "pdf":
                    (documentBytes, fileName, contentType) = await GeneratePdfAsync(businessPlan, message.Language, cancellationToken);
                    break;
                case "word":
                case "docx":
                    (documentBytes, fileName, contentType) = await GenerateWordAsync(businessPlan, message.Language, cancellationToken);
                    break;
                default:
                    _logger.LogError("Unsupported export type: {ExportType}", message.ExportType);
                    return false;
            }

            // Upload to S3
            var s3Key = $"exports/{message.BusinessPlanId}/{fileName}";
            await UploadToS3Async(s3Key, documentBytes, contentType, cancellationToken);

            // Update job status in database (if you have a jobs table)
            await UpdateExportJobStatusAsync(message.JobId, "completed", s3Key, cancellationToken);

            _logger.LogInformation(
                "Successfully completed export job {JobId}. File uploaded to S3: {S3Key}",
                message.JobId,
                s3Key);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing export job {JobId} for business plan {BusinessPlanId}",
                message.JobId,
                message.BusinessPlanId);

            // Update job status to failed
            try
            {
                await UpdateExportJobStatusAsync(message.JobId, "failed", null, cancellationToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update export job status to failed");
            }

            throw;
        }
    }

    private async Task<BusinessPlanData?> GetBusinessPlanAsync(string businessPlanId, CancellationToken cancellationToken)
    {
        var connectionString = BuildConnectionString();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            SELECT ""Id"", ""Title"", ""ExecutiveSummary"", ""ProblemStatement"", ""Solution"",
                   ""MarketAnalysis"", ""CompetitiveAnalysis"", ""FinancialProjections"",
                   ""MarketingStrategy"", ""ManagementTeam""
            FROM ""BusinessPlans""
            WHERE ""Id"" = @Id AND ""IsDeleted"" = false";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", Guid.Parse(businessPlanId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new BusinessPlanData
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                ExecutiveSummary = reader.IsDBNull(2) ? null : reader.GetString(2),
                ProblemStatement = reader.IsDBNull(3) ? null : reader.GetString(3),
                Solution = reader.IsDBNull(4) ? null : reader.GetString(4),
                MarketAnalysis = reader.IsDBNull(5) ? null : reader.GetString(5),
                CompetitiveAnalysis = reader.IsDBNull(6) ? null : reader.GetString(6),
                FinancialProjections = reader.IsDBNull(7) ? null : reader.GetString(7),
                MarketingStrategy = reader.IsDBNull(8) ? null : reader.GetString(8),
                ManagementTeam = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
        }

        return null;
    }

    private async Task<(byte[] bytes, string fileName, string contentType)> GeneratePdfAsync(
        BusinessPlanData businessPlan,
        string language,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating PDF for business plan {BusinessPlanId}", businessPlan.Id);

        var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text($"{GetLocalizedText("Business Plan", language)}: {businessPlan.Title}")
                    .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);
                        AddSection(x, GetLocalizedText("Executive Summary", language), businessPlan.ExecutiveSummary);
                        AddSection(x, GetLocalizedText("Problem Statement", language), businessPlan.ProblemStatement);
                        AddSection(x, GetLocalizedText("Solution", language), businessPlan.Solution);
                        AddSection(x, GetLocalizedText("Market Analysis", language), businessPlan.MarketAnalysis);
                        AddSection(x, GetLocalizedText("Competitive Analysis", language), businessPlan.CompetitiveAnalysis);
                        AddSection(x, GetLocalizedText("Financial Projections", language), businessPlan.FinancialProjections);
                        AddSection(x, GetLocalizedText("Marketing Strategy", language), businessPlan.MarketingStrategy);
                        AddSection(x, GetLocalizedText("Management Team", language), businessPlan.ManagementTeam);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span($"{GetLocalizedText("Generated on", language)}: ");
                        x.Span(DateTime.Now.ToString("MMMM dd, yyyy"));
                    });
            });
        })
        .GeneratePdf();

        var fileName = $"{SanitizeFileName(businessPlan.Title)}_{language}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        return (pdfBytes, fileName, "application/pdf");
    }

    private async Task<(byte[] bytes, string fileName, string contentType)> GenerateWordAsync(
        BusinessPlanData businessPlan,
        string language,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating Word document for business plan {BusinessPlanId}", businessPlan.Id);

        // Word document generation using DocumentFormat.OpenXml
        // Note: Full implementation pending - currently returns placeholder
        var content = new StringBuilder();
        content.AppendLine($"{GetLocalizedText("Business Plan", language)}: {businessPlan.Title}");
        content.AppendLine();
        AddTextSection(content, GetLocalizedText("Executive Summary", language), businessPlan.ExecutiveSummary);
        AddTextSection(content, GetLocalizedText("Problem Statement", language), businessPlan.ProblemStatement);
        AddTextSection(content, GetLocalizedText("Solution", language), businessPlan.Solution);

        var wordBytes = Encoding.UTF8.GetBytes(content.ToString());
        var fileName = $"{SanitizeFileName(businessPlan.Title)}_{language}_{DateTime.UtcNow:yyyyMMdd}.docx";
        
        // Note: Full Word document generation implementation pending
        return (wordBytes, fileName, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    private async Task UploadToS3Async(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _config.S3BucketName,
            Key = key,
            InputStream = new MemoryStream(content),
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        _logger.LogInformation("Uploaded file to S3: {S3Key}", key);
    }

    private async Task UpdateExportJobStatusAsync(string jobId, string status, string? s3Key, CancellationToken cancellationToken)
    {
        // Export job status tracking
        // Note: Full implementation pending - export jobs table to be added
        _logger.LogInformation("Export job {JobId} status updated to {Status}. S3Key: {S3Key}", jobId, status, s3Key);
        await Task.CompletedTask;
    }

    private void AddSection(ColumnDescriptor column, string heading, string? content)
    {
        if (string.IsNullOrEmpty(content)) return;

        column.Item().Text(heading).SemiBold().FontSize(14);
        column.Item().Text(content);
    }

    private void AddTextSection(StringBuilder sb, string heading, string? content)
    {
        if (string.IsNullOrEmpty(content)) return;
        sb.AppendLine(heading);
        sb.AppendLine(new string('=', heading.Length));
        sb.AppendLine(content);
        sb.AppendLine();
    }

    private string GetLocalizedText(string key, string language)
    {
        var translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["Business Plan"] = new() { ["fr"] = "Plan d'Affaires", ["en"] = "Business Plan" },
            ["Executive Summary"] = new() { ["fr"] = "Résumé Exécutif", ["en"] = "Executive Summary" },
            ["Problem Statement"] = new() { ["fr"] = "Énoncé du Problème", ["en"] = "Problem Statement" },
            ["Solution"] = new() { ["fr"] = "Solution", ["en"] = "Solution" },
            ["Market Analysis"] = new() { ["fr"] = "Analyse de Marché", ["en"] = "Market Analysis" },
            ["Competitive Analysis"] = new() { ["fr"] = "Analyse Concurrentielle", ["en"] = "Competitive Analysis" },
            ["Financial Projections"] = new() { ["fr"] = "Projections Financières", ["en"] = "Financial Projections" },
            ["Marketing Strategy"] = new() { ["fr"] = "Stratégie Marketing", ["en"] = "Marketing Strategy" },
            ["Management Team"] = new() { ["fr"] = "Équipe de Direction", ["en"] = "Management Team" },
            ["Generated on"] = new() { ["fr"] = "Généré le", ["en"] = "Generated on" }
        };

        if (translations.TryGetValue(key, out var translation) &&
            translation.TryGetValue(language, out var text))
        {
            return text;
        }

        return key;
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private string BuildConnectionString()
    {
        return $"Host={_config.RdsEndpoint};" +
               $"Database={_config.DatabaseName};" +
               $"Username={_config.DatabaseUsername};" +
               $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? ""};" +
               $"SSL Mode=Require;";
    }
}

/// <summary>
/// Business plan data model for export processing
/// </summary>
public class BusinessPlanData
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ExecutiveSummary { get; set; }
    public string? ProblemStatement { get; set; }
    public string? Solution { get; set; }
    public string? MarketAnalysis { get; set; }
    public string? CompetitiveAnalysis { get; set; }
    public string? FinancialProjections { get; set; }
    public string? MarketingStrategy { get; set; }
    public string? ManagementTeam { get; set; }
}

