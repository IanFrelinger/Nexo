using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces;

/// <summary>
/// Interface for managing AI configuration settings
/// </summary>
public interface IAiConfigurationService
{
    /// <summary>
    /// Gets the current AI configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current AI configuration</returns>
    Task<AiConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the AI configuration
    /// </summary>
    /// <param name="configuration">The new configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the update operation</returns>
    Task UpdateConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the AI configuration
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<AiConfigurationValidationResult> ValidateConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the configuration to defaults
    /// </summary>
    /// <param name="mode">The AI mode to use for defaults</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the reset operation</returns>
    Task ResetToDefaultsAsync(AiMode mode, CancellationToken cancellationToken = default);
}
