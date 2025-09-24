using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Networking.Models;

namespace Nexo.Feature.Networking.Interfaces;

/// <summary>
/// Interface for networking/multiplayer providers
/// </summary>
public interface INetworkingProvider
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
    /// Generates networking configuration based on the provided request
    /// </summary>
    /// <param name="request">Networking request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated networking result</returns>
    Task<NetworkingResult> GenerateNetworkingConfigurationAsync(NetworkingRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates server code based on the configuration
    /// </summary>
    /// <param name="configuration">Networking configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated server code</returns>
    Task<string> GenerateServerCodeAsync(NetworkingConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates client code based on the configuration
    /// </summary>
    /// <param name="configuration">Networking configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated client code</returns>
    Task<string> GenerateClientCodeAsync(NetworkingConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates multiple networking configurations in batch
    /// </summary>
    /// <param name="requests">Multiple networking requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of generated networking results</returns>
    Task<IEnumerable<NetworkingResult>> GenerateNetworkingBatchAsync(IEnumerable<NetworkingRequest> requests, CancellationToken cancellationToken = default);
}

