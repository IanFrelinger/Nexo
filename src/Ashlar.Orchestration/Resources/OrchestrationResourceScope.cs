using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Resources;

namespace Ashlar.Orchestration.Resources;

/// <summary>
/// Tracks provisioned resources for a single orchestration scope and disposes them in a defined order.
/// Register as scoped; resolve alongside <see cref="Coordination.Orchestrator"/> for the same logical run.
/// </summary>
public sealed class OrchestrationResourceScope : IAsyncDisposable
{
    private readonly ResourceTeardownPolicy _teardownPolicy;
    private readonly ILogger<OrchestrationResourceScope>? _logger;
    private readonly List<IProvisionedResource> _agents = new();
    private readonly List<IProvisionedResource> _databases = new();
    private int _disposed;

    public OrchestrationResourceScope(
        ResourceTeardownPolicy teardownPolicy = ResourceTeardownPolicy.AgentsBeforeDatabases,
        ILogger<OrchestrationResourceScope>? logger = null)
    {
        _teardownPolicy = teardownPolicy;
        _logger = logger;
    }

    /// <summary>
    /// Registers a resource for teardown. Not thread-safe; call from orchestration flow only.
    /// </summary>
    public void Track(IProvisionedResource resource)
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }

        switch (resource.Kind)
        {
            case ProvisionedResourceKind.AgentHandle:
                _agents.Add(resource);
                break;
            case ProvisionedResourceKind.IsolatedDatabase:
                _databases.Add(resource);
                break;
            case ProvisionedResourceKind.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(resource), resource.Kind, "Unknown provisioned resource kind.");
        }
    }

    public IReadOnlyList<IProvisionedResource> AgentResources => _agents;

    public IReadOnlyList<IProvisionedResource> DatabaseResources => _databases;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        IEnumerable<IProvisionedResource> ordered = _teardownPolicy switch
        {
            ResourceTeardownPolicy.AgentsBeforeDatabases => OrderReverse(_agents).Concat(OrderReverse(_databases)),
            ResourceTeardownPolicy.DatabasesBeforeAgents => OrderReverse(_databases).Concat(OrderReverse(_agents)),
            _ => OrderReverse(_agents).Concat(OrderReverse(_databases))
        };

        foreach (var r in ordered)
        {
            try
            {
                await r.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Dispose failed for provisioned resource kind {Kind}", r.Kind);
            }
        }

        _agents.Clear();
        _databases.Clear();
    }

    private static IEnumerable<IProvisionedResource> OrderReverse(List<IProvisionedResource> list)
    {
        for (var i = list.Count - 1; i >= 0; i--)
        {
            yield return list[i];
        }
    }
}
