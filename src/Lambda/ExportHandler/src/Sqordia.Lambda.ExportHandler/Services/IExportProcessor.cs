using Sqordia.Lambda.ExportHandler.Models;

namespace Sqordia.Lambda.ExportHandler.Services;

/// <summary>
/// Service for processing document export jobs
/// </summary>
public interface IExportProcessor
{
    /// <summary>
    /// Process an export job message
    /// </summary>
    Task<bool> ProcessExportJobAsync(ExportJobMessage message, CancellationToken cancellationToken = default);
}

