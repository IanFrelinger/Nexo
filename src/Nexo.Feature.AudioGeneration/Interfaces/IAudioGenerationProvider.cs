using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AudioGeneration.Models;

namespace Nexo.Feature.AudioGeneration.Interfaces;

/// <summary>
/// Interface for audio generation providers
/// </summary>
public interface IAudioGenerationProvider
{
    /// <summary>
    /// Unique identifier for this provider
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Whether this provider requires online connectivity
    /// </summary>
    bool RequiresOnline { get; }
    
    /// <summary>
    /// Whether this provider is currently available
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Gets all available models from this provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of available audio generation models</returns>
    Task<IEnumerable<AudioGenerationModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates audio based on the provided request
    /// </summary>
    /// <param name="request">Audio generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated audio result</returns>
    Task<AudioGenerationResult> GenerateAudioAsync(AudioGenerationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates audio with a specific model
    /// </summary>
    /// <param name="model">Model to use for generation</param>
    /// <param name="request">Audio generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated audio result</returns>
    Task<AudioGenerationResult> GenerateAudioAsync(AudioGenerationModel model, AudioGenerationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates multiple audio clips in batch
    /// </summary>
    /// <param name="requests">Multiple audio generation requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of generated audio results</returns>
    Task<IEnumerable<AudioGenerationResult>> GenerateAudioBatchAsync(IEnumerable<AudioGenerationRequest> requests, CancellationToken cancellationToken = default);
}
