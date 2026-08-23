using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Agents;
using Ashlar.Abstractions.Database;

namespace Ashlar.Orchestration.Resources;

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
