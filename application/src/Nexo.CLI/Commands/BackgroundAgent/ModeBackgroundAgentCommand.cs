using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command to get or set the background agent aggressiveness mode.
/// Mode is switchable at runtime without restart.
/// </summary>
public class ModeBackgroundAgentCommand
{
    private readonly IAggressivenessModeStore _modeStore;
    private readonly ILogger<ModeBackgroundAgentCommand> _logger;

    /// <summary>Creates a new ModeBackgroundAgentCommand instance.</summary>
    public ModeBackgroundAgentCommand(
        IAggressivenessModeStore modeStore,
        ILogger<ModeBackgroundAgentCommand> logger)
    {
        _modeStore = modeStore ?? throw new ArgumentNullException(nameof(modeStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the current aggressiveness mode.
    /// </summary>
    public Task<int> GetAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var mode = _modeStore.GetMode();
            var modeStr = ToModeString(mode);
            if (formatJson)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, mode = modeStr }, options));
            }
            else
            {
                Console.Out.WriteLine($"Background agent mode: {modeStr}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get mode failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Set the aggressiveness mode.
    /// </summary>
    public Task<int> SetAsync(string mode, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var parsed = ParseMode(mode);
            _modeStore.SetMode(parsed);
            var modeStr = ToModeString(parsed);
            if (formatJson)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, mode = modeStr }, options));
            }
            else
            {
                Console.Out.WriteLine($"Background agent mode set to: {modeStr}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set mode failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private static BackgroundAgentAggressivenessMode ParseMode(string mode)
    {
        return mode?.ToLowerInvariant() switch
        {
            "passive" => BackgroundAgentAggressivenessMode.Passive,
            "semi-active" or "semiactive" => BackgroundAgentAggressivenessMode.SemiActive,
            "active" => BackgroundAgentAggressivenessMode.Active,
            "ambient" => BackgroundAgentAggressivenessMode.Ambient,
            _ => throw new ArgumentException($"Unknown mode: {mode}. Use: passive, semi-active, active, ambient")
        };
    }

    private static string ToModeString(BackgroundAgentAggressivenessMode mode)
    {
        return mode switch
        {
            BackgroundAgentAggressivenessMode.Passive => "passive",
            BackgroundAgentAggressivenessMode.SemiActive => "semi-active",
            BackgroundAgentAggressivenessMode.Active => "active",
            BackgroundAgentAggressivenessMode.Ambient => "ambient",
            _ => mode.ToString().ToLowerInvariant()
        };
    }
}
