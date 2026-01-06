using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Manages execution state for active sessions.
/// </summary>
public interface IExecutionStateManager
{
    void SetProvider(string connectionId, string provider);
    string GetCurrentProvider(string connectionId);
    void SetOffline(string connectionId, bool offline);
    bool IsOffline(string connectionId);
    void SetBrickImplementation(string connectionId, string brickId, ImplementationType implementation);
    ImplementationType? GetBrickImplementation(string connectionId, string brickId);
}

