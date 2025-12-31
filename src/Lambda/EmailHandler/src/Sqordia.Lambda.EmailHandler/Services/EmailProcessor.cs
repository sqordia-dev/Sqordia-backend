using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sqordia.Lambda.EmailHandler.Configuration;
using Sqordia.Lambda.EmailHandler.Models;

namespace Sqordia.Lambda.EmailHandler.Services;

/// <summary>
/// Service implementation for processing email jobs via AWS SES
/// </summary>
public class EmailProcessor : IEmailProcessor
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly ILogger<EmailProcessor> _logger;
    private readonly LambdaConfiguration _config;

    public EmailProcessor(
        IAmazonSimpleEmailService sesClient,
        ILogger<EmailProcessor> logger,
        IOptions<LambdaConfiguration> config)
    {
        _sesClient = sesClient;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<bool> ProcessEmailJobAsync(EmailJobMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing email job {JobId} of type {EmailType} to {ToEmail}",
                message.JobId,
                message.EmailType,
                message.ToEmail);

            var sendRequest = new SendEmailRequest
            {
                Source = $"{_config.SesFromName} <{_config.SesFromEmail}>",
                Destination = new Destination
                {
                    ToAddresses = new List<string> { message.ToEmail }
                },
                Message = new Message
                {
                    Subject = new Content(message.Subject),
                    Body = new Body
                    {
                        Text = !string.IsNullOrEmpty(message.Body)
                            ? new Content(message.Body)
                            : null,
                        Html = !string.IsNullOrEmpty(message.HtmlBody)
                            ? new Content(message.HtmlBody)
                            : new Content($"<p>{message.Body}</p>")
                    }
                }
            };

            var response = await _sesClient.SendEmailAsync(sendRequest, cancellationToken);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation(
                    "Successfully sent email job {JobId} to {ToEmail}. MessageId: {MessageId}",
                    message.JobId,
                    message.ToEmail,
                    response.MessageId);
                return true;
            }

            _logger.LogWarning(
                "Failed to send email job {JobId} to {ToEmail}. StatusCode: {StatusCode}",
                message.JobId,
                message.ToEmail,
                response.HttpStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing email job {JobId} to {ToEmail}",
                message.JobId,
                message.ToEmail);
            throw;
        }
    }
}

