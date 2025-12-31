using System.Text.Json.Serialization;

namespace Sqordia.Lambda.EmailHandler.Models;

/// <summary>
/// Email job message from SQS queue
/// </summary>
public class EmailJobMessage
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("emailType")]
    public string EmailType { get; set; } = string.Empty;

    [JsonPropertyName("toEmail")]
    public string ToEmail { get; set; } = string.Empty;

    [JsonPropertyName("toName")]
    public string? ToName { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("htmlBody")]
    public string? HtmlBody { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

