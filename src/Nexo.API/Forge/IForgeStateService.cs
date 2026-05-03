using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Session;

namespace Nexo.API.Forge;

/// <summary>
/// Holds the mutable Forge session and macro registry for the API process.
/// </summary>
public interface IForgeStateService
{
    SessionState Session { get; set; }

    MacroRegistry Registry { get; set; }

    /// <summary>
    /// Persists the current session and macros when a durable store is enabled; otherwise no-op.
    /// </summary>
    void Save();
}
