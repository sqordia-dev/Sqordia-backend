using Sqordia.Lambda.EmailHandler.Models;

namespace Sqordia.Lambda.EmailHandler.Services;

/// <summary>
/// Service for processing email jobs
/// </summary>
public interface IEmailProcessor
{
    /// <summary>
    /// Process an email job message
    /// </summary>
    Task<bool> ProcessEmailJobAsync(EmailJobMessage message, CancellationToken cancellationToken = default);
}

