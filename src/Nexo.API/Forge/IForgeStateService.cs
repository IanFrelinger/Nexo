using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Session;

namespace Nexo.API.Forge;

public interface IForgeStateService
{
    SessionState Session { get; set; }

    MacroRegistry Registry { get; set; }

    void Save();
}
