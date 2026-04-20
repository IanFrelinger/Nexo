using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using Nexo.Abstractions.Resources;

namespace Nexo.Orchestration.Resources;

/// <summary>
/// Wraps an <see cref="IAgentHandle"/> as a <see cref="IProvisionedResource"/> for unified teardown.
/// Prefer tracking agents that are not already shut down by <see cref="LifecycleManager"/> to avoid redundant work.
/// </summary>
public sealed class ProvisionedAgentResource : IProvisionedResource
{
    private readonly IAgentHandle _handle;
    private readonly ILogger<ProvisionedAgentResource>? _logger;

    public ProvisionedAgentResource(IAgentHandle handle, ILogger<ProvisionedAgentResource>? logger = null)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _logger = logger;
    }

    public ProvisionedResourceKind Kind => ProvisionedResourceKind.AgentHandle;

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _handle.ShutdownAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Agent {AgentId} shutdown failed; terminating", _handle.AgentId);
            _handle.Terminate();
        }
    }
}
