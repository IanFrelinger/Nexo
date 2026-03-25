using Grpc.Net.Client;

namespace Nexo.Transport.Grpc;

/// <summary>
/// Factory abstraction for creating and caching gRPC channels by endpoint.
/// </summary>
public interface IGrpcChannelFactory
{
    /// <summary>
    /// Gets an existing channel for an endpoint, or creates one if it does not exist.
    /// </summary>
    /// <param name="endpoint">The endpoint URI.</param>
    /// <returns>A gRPC channel for the endpoint.</returns>
    GrpcChannel GetOrCreate(string endpoint);

    /// <summary>
    /// Disposes all currently cached channels and clears the cache.
    /// </summary>
    void DisposeAll();
}
