using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Animation.Models;

namespace Nexo.Feature.Animation.Interfaces;

/// <summary>
/// Interface for animation generation providers
/// </summary>
public interface IAnimationProvider
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
    /// Generates animation based on the provided request
    /// </summary>
    /// <param name="request">Animation generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated animation result</returns>
    Task<AnimationGenerationResult> GenerateAnimationAsync(AnimationGenerationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates rig based on the provided request
    /// </summary>
    /// <param name="request">Rigging request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated rig result</returns>
    Task<RiggingResult> GenerateRigAsync(RiggingRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates multiple animations in batch
    /// </summary>
    /// <param name="requests">Multiple animation generation requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of generated animation results</returns>
    Task<IEnumerable<AnimationGenerationResult>> GenerateAnimationBatchAsync(IEnumerable<AnimationGenerationRequest> requests, CancellationToken cancellationToken = default);
}
