using Nexo.Agents.UniversalTester.Configuration;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Registry for creating target adapters by type.
/// Enables configurable adapter params (host, port) and future custom adapters.
/// </summary>
public interface IAdapterRegistry
{
    /// <summary>
    /// Creates an adapter for the given target type.
    /// </summary>
    /// <param name="type">Target type (WebApp, Game, Api, Cli, DesktopApp).</param>
    /// <param name="config">Optional per-adapter config (e.g. host, port for Game).</param>
    /// <returns>A new adapter instance.</returns>
    ITargetAdapter CreateAdapter(TargetType type, IReadOnlyDictionary<string, object>? config = null);

    /// <summary>
    /// Target types this registry can create.
    /// </summary>
    IReadOnlyList<TargetType> GetRegisteredTypes();
}
