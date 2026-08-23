using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;

namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// File-based store for aggressiveness mode. Persists to disk so that
/// <c>ashlar background-agent mode set</c> is respected by a running background
/// agent process without restart. Reads from file on each GetMode() so the
/// next execution cycle picks up changes.
///
/// <para><b>Fail-closed to Passive.</b> A missing file, unreadable JSON, a document
/// with no <c>Mode</c>, or a value the store does not recognise all resolve to
/// <see cref="BackgroundAgentAggressivenessMode.Passive"/> — the mode in which the
/// extender takes no action. Only an explicit, recognised value can arm anything;
/// <c>{}</c> or a typo must never read as Active. The effective mode is logged when it
/// changes and every fallback is logged as a warning, so an operator who meant to arm
/// the loop and did not can see why.</para>
/// </summary>
public sealed class FileBasedAggressivenessModeStore : IAggressivenessModeStore
{
    private readonly string _path;
    private readonly ILogger<FileBasedAggressivenessModeStore>? _logger;
    private readonly object _sync = new();
    private BackgroundAgentAggressivenessMode? _lastLogged;

    /// <summary>
    /// Creates a file-based store. Default path: ~/.ashlar/agent-mode.json.
    /// Use ASHLAR_AGENT_MODE_PATH to override.
    /// </summary>
    /// <param name="path">Mode file path; null uses the default under the user profile.</param>
    /// <param name="logger">Optional logger for the effective mode and every fallback.</param>
    public FileBasedAggressivenessModeStore(
        string? path = null,
        ILogger<FileBasedAggressivenessModeStore>? logger = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ashlar",
            "agent-mode.json");
        _logger = logger;
    }

    /// <inheritdoc />
    public BackgroundAgentAggressivenessMode GetMode()
    {
        BackgroundAgentAggressivenessMode mode;
        try
        {
            if (!File.Exists(_path))
            {
                mode = BackgroundAgentAggressivenessMode.Passive;
            }
            else
            {
                var json = File.ReadAllText(_path);
                var doc = JsonSerializer.Deserialize<ModeDoc>(json);
                var modeStr = doc?.Mode;
                if (string.IsNullOrWhiteSpace(modeStr))
                {
                    _logger?.LogWarning(
                        "Aggressiveness mode file {Path} has no 'Mode' value; falling back to Passive (fail-closed)",
                        _path);
                    mode = BackgroundAgentAggressivenessMode.Passive;
                }
                else if (TryParseMode(modeStr, out var parsed))
                {
                    mode = parsed;
                }
                else
                {
                    _logger?.LogWarning(
                        "Aggressiveness mode file {Path} has unrecognised Mode '{Mode}' (expected passive, semi-active, "
                        + "active or ambient); falling back to Passive (fail-closed)",
                        _path, modeStr);
                    mode = BackgroundAgentAggressivenessMode.Passive;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "Aggressiveness mode file {Path} could not be read; falling back to Passive (fail-closed)", _path);
            mode = BackgroundAgentAggressivenessMode.Passive;
        }

        LogEffectiveModeIfChanged(mode);
        return mode;
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
            _ => "passive"
        };
        var doc = new ModeDoc { Mode = modeStr };
        File.WriteAllText(_path, JsonSerializer.Serialize(doc));
    }

    private static bool TryParseMode(string mode, out BackgroundAgentAggressivenessMode parsed)
    {
        switch (mode.Trim().ToLowerInvariant())
        {
            case "passive":
                parsed = BackgroundAgentAggressivenessMode.Passive;
                return true;
            case "semi-active":
            case "semiactive":
                parsed = BackgroundAgentAggressivenessMode.SemiActive;
                return true;
            case "active":
                parsed = BackgroundAgentAggressivenessMode.Active;
                return true;
            case "ambient":
                parsed = BackgroundAgentAggressivenessMode.Ambient;
                return true;
            default:
                parsed = BackgroundAgentAggressivenessMode.Passive;
                return false;
        }
    }

    // GetMode runs every cycle; the effective mode is worth a line when it CHANGES, not
    // every time it is read.
    private void LogEffectiveModeIfChanged(BackgroundAgentAggressivenessMode mode)
    {
        if (_logger is null)
            return;

        lock (_sync)
        {
            if (_lastLogged == mode)
                return;
            _lastLogged = mode;
        }

        _logger.LogInformation("Background agent aggressiveness mode: {Mode} (from {Path})", mode, _path);
    }

    private sealed class ModeDoc
    {
        public string? Mode { get; set; }
    }
}
