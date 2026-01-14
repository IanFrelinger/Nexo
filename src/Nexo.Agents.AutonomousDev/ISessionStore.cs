using Nexo.Agents.AutonomousDev.Models;

namespace Nexo.Agents.AutonomousDev;

/// <summary>
/// Optional persistence hook for saving development sessions as they progress.
/// </summary>
public interface ISessionStore
{
    Task SaveAsync(DevelopmentSession session, CancellationToken ct = default);
}

