using System.Text.Json.Serialization;

namespace Sqordia.Lambda.AIGenerationHandler.Models;

/// <summary>
/// AI generation job message from SQS queue
/// </summary>
public class AIGenerationJobMessage
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("businessPlanId")]
    public string BusinessPlanId { get; set; } = string.Empty;

    [JsonPropertyName("planType")]
    public string PlanType { get; set; } = "standard";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "fr";

    [JsonPropertyName("sections")]
    public List<string>? Sections { get; set; }

    [JsonPropertyName("questionnaireContext")]
    public Dictionary<string, object>? QuestionnaireContext { get; set; }

    [JsonPropertyName("aiProvider")]
    public string? AiProvider { get; set; }
}

