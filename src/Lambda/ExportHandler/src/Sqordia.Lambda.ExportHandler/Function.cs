using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqordia.Lambda.ExportHandler.Models;
using Sqordia.Lambda.ExportHandler.Services;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Sqordia.Lambda.ExportHandler;

/// <summary>
/// Lambda function handler for processing document export jobs from SQS queue
/// </summary>
public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Function> _logger;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance.
    /// </summary>
    public Function()
    {
        _serviceProvider = Startup.ConfigureServices();
        _logger = _serviceProvider.GetRequiredService<ILogger<Function>>();
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object
    /// and processes document export jobs from the queue.
    /// </summary>
    /// <param name="evnt">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        _logger.LogInformation(
            "Processing {RecordCount} export job(s) from SQS queue",
            evnt.Records.Count);

        using var scope = _serviceProvider.CreateScope();
        var exportProcessor = scope.ServiceProvider.GetRequiredService<IExportProcessor>();

        var successCount = 0;
        var failureCount = 0;

        foreach (var record in evnt.Records)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ExportJobMessage>(record.Body);
                if (message == null)
                {
                    _logger.LogWarning("Failed to deserialize export job message. MessageId: {MessageId}", record.MessageId);
                    failureCount++;
                    continue;
                }

                var success = await exportProcessor.ProcessExportJobAsync(message, context.CancellationToken);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                    // Message will be retried or moved to DLQ by SQS
                    throw new Exception($"Failed to process export job {message.JobId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing export job. MessageId: {MessageId}, ReceiptHandle: {ReceiptHandle}",
                    record.MessageId,
                    record.ReceiptHandle);
                failureCount++;
                // Re-throw to allow SQS to handle retry/DLQ logic
                throw;
            }
        }

        _logger.LogInformation(
            "Completed processing export jobs. Success: {SuccessCount}, Failures: {FailureCount}",
            successCount,
            failureCount);
    }
}
