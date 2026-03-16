using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Routing;

namespace Nexo.Runtime.Routing;

/// <summary>
/// Thread-safe in-memory endpoint registry.
/// </summary>
public sealed class InMemoryEndpointRegistry : IEndpointRegistry
{
    private readonly ConcurrentDictionary<string, EndpointDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InMemoryEndpointRegistry> _logger;

    public InMemoryEndpointRegistry(
        IOptions<RoutingOptions> options,
        ILogger<InMemoryEndpointRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var routingOptions = options?.Value ?? new RoutingOptions();

        foreach (var endpoint in routingOptions.Endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Endpoint))
            {
                continue;
            }

            Register(endpoint.ToDescriptor() with { IsHealthy = true });
        }
    }

    public IReadOnlyList<EndpointDescriptor> GetAll()
    {
        return _descriptors.Values
            .OrderBy(descriptor => descriptor.Priority)
            .ThenBy(descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void Register(EndpointDescriptor descriptor)
    {
        if (descriptor is null)
            throw new ArgumentNullException(nameof(descriptor));
        _descriptors[descriptor.Endpoint] = descriptor;
    }

    public void UpdateHealth(string endpoint, bool isHealthy)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        if (_descriptors.TryGetValue(endpoint, out var descriptor))
        {
            _descriptors[endpoint] = descriptor with { IsHealthy = isHealthy };
            return;
        }

        _logger.LogWarning("Cannot update health for unknown endpoint {Endpoint}", endpoint);
    }
}
