using Nexo.Commercial.GameDomain.Macros;
using Nexo.Commercial.GameDomain.Session;

namespace GameDirector.Mcp.Forge;
/// <summary>Contract for forge state service.</summary>
public interface IForgeStateService
{
    /// <summary>Session.</summary>
    SessionState Session { get; set; }

    /// <summary>Registry.</summary>
    MacroRegistry Registry { get; set; }

    /// <summary>Save.</summary>
    void Save();
}
