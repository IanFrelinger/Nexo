using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;

namespace Nexo.Agents.UniversalTester.Adapters;

/// <summary>
/// Default adapter registry with built-in adapters. Supports config overrides for host/port.
/// </summary>
public sealed class DefaultAdapterRegistry : IAdapterRegistry
{
    private readonly ILoggerFactory _loggerFactory;

    public DefaultAdapterRegistry(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? LoggerFactory.Create(builder => builder.AddConsole());
    }

    /// <summary>Creates the appropriate adapter for the target type. Config can override host/port for Game adapter.</summary>
    public ITargetAdapter CreateAdapter(TargetType type, IReadOnlyDictionary<string, object>? config = null)
    {
        return type switch
        {
            TargetType.WebApp => new WebAdapter(_loggerFactory.CreateLogger<WebAdapter>()),
            TargetType.Game => CreateGameAdapter(config),
            TargetType.Api => new ApiAdapter(_loggerFactory.CreateLogger<ApiAdapter>()),
            TargetType.Cli => new CliAdapter(_loggerFactory.CreateLogger<CliAdapter>()),
            TargetType.DesktopApp => new DesktopAdapter(_loggerFactory.CreateLogger<DesktopAdapter>()),
            _ => new WebAdapter(_loggerFactory.CreateLogger<WebAdapter>())
        };
    }

    /// <summary>Returns the list of target types this registry can create adapters for.</summary>
    public IReadOnlyList<TargetType> GetRegisteredTypes() =>
        [TargetType.WebApp, TargetType.Game, TargetType.Api, TargetType.Cli, TargetType.DesktopApp];

    /// <summary>Creates GameAdapter with optional host/port from config (adapters.game in agent spec).</summary>
    private ITargetAdapter CreateGameAdapter(IReadOnlyDictionary<string, object>? config)
    {
        string? host = null;
        int? port = null;
        if (config != null)
        {
            if (config.TryGetValue("host", out var h) && h is string hs)
                host = hs;
            if (config.TryGetValue("port", out var p))
                port = p switch { int i => i, long l => (int)l, string s when int.TryParse(s, out var v) => v, _ => null };
        }
        return new GameAdapter(_loggerFactory.CreateLogger<GameAdapter>(), host, port);
    }
}
