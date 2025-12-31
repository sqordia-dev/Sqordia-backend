using Sqordia.Lambda.AIGenerationHandler.Models;

namespace Sqordia.Lambda.AIGenerationHandler.Services;

/// <summary>
/// Service for processing AI business plan generation jobs
/// </summary>
public interface IAIGenerationProcessor
{
    /// <summary>
    /// Process an AI generation job message
    /// </summary>
    Task<bool> ProcessGenerationJobAsync(AIGenerationJobMessage message, CancellationToken cancellationToken = default);
}

