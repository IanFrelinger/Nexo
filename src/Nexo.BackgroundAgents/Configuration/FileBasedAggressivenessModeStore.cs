using System.Text.Json;

namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// File-based store for aggressiveness mode. Persists to disk so that
/// <c>nexo background-agent mode set</c> is respected by a running background
/// agent process without restart. Reads from file on each GetMode() so the
/// next execution cycle picks up changes.
/// </summary>
public sealed class FileBasedAggressivenessModeStore : IAggressivenessModeStore
{
    private readonly string _path;

    /// <summary>
    /// Creates a file-based store. Default path: ~/.nexo/agent-mode.json.
    /// Use NEXO_AGENT_MODE_PATH to override.
    /// </summary>
    public FileBasedAggressivenessModeStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nexo",
            "agent-mode.json");
    }

    /// <inheritdoc />
    public BackgroundAgentAggressivenessMode GetMode()
    {
        try
        {
            if (!File.Exists(_path))
                return BackgroundAgentAggressivenessMode.Active;

            var json = File.ReadAllText(_path);
            var doc = JsonSerializer.Deserialize<ModeDoc>(json);
            var modeStr = doc?.Mode ?? "active";
            return ParseMode(modeStr);
        }
        catch
        {
            return BackgroundAgentAggressivenessMode.Active;
        }
    }

    /// <inheritdoc />
    public void SetMode(BackgroundAgentAggressivenessMode mode)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var modeStr = mode switch
        {
            BackgroundAgentAggressivenessMode.Passive => "passive",
            BackgroundAgentAggressivenessMode.SemiActive => "semi-active",
            BackgroundAgentAggressivenessMode.Active => "active",
            BackgroundAgentAggressivenessMode.Ambient => "ambient",
            _ => "active"
        };
        var doc = new ModeDoc { Mode = modeStr };
        File.WriteAllText(_path, JsonSerializer.Serialize(doc));
    }

    private static BackgroundAgentAggressivenessMode ParseMode(string mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            "passive" => BackgroundAgentAggressivenessMode.Passive,
            "semi-active" or "semiactive" => BackgroundAgentAggressivenessMode.SemiActive,
            "active" => BackgroundAgentAggressivenessMode.Active,
            "ambient" => BackgroundAgentAggressivenessMode.Ambient,
            _ => BackgroundAgentAggressivenessMode.Active
        };
    }

    private sealed class ModeDoc
    {
        public string Mode { get; set; } = "active";
    }
}
