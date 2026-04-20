using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using Nexo.Abstractions.Database;

namespace Nexo.Orchestration.Resources;

/// <summary>
/// Convenience helpers for tracking common resource types on <see cref="OrchestrationResourceScope"/>.
/// </summary>
public static class OrchestrationResourceScopeExtensions
{
    public static void TrackIsolatedDatabase(this OrchestrationResourceScope scope, IIsolatedDatabase database)
    {
        scope.Track(new ProvisionedDatabaseResource(database));
    }

    public static void TrackAgentHandle(
        this OrchestrationResourceScope scope,
        IAgentHandle handle,
        ILogger<ProvisionedAgentResource>? logger = null)
    {
        scope.Track(new ProvisionedAgentResource(handle, logger));
    }
}
