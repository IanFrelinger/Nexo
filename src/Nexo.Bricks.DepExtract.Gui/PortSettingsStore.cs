using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Bricks.DepExtract;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// Operator-chosen host ports when defaults (18080 / 5237) collide.
/// Precedence: environment → persisted override → built-in defaults.
/// </summary>
public static class PortSettingsStore
{
    public const int DefaultParserHostPort = 18080;
    public const int DefaultAgentHostPort = 5237;

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string ResolveSettingsPath()
    {
        var tmpBase = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(tmpBase))
        {
            tmpBase = DepExtractPathRules.TryGetWorkspaceRoot() is { } ws
                ? Path.Combine(ws, ".dep-extract-out")
                : Path.Combine(Path.GetTempPath(), "dep-extract-out");
        }

        Directory.CreateDirectory(tmpBase);
        return Path.Combine(tmpBase, "ports.json");
    }

    /// <summary>Compose project this agent instance belongs to (process env).</summary>
    public static string ResolveComposeProject()
        => Environment.GetEnvironmentVariable("COMPOSE_PROJECT")
           ?? Environment.GetEnvironmentVariable("COMPOSE_PROJECT_NAME")
           ?? "nexo-parser-pipeline";

    /// <summary>
    /// Per-project env: <c>$WORKSPACE_ROOT/.parser-pipeline/&lt;COMPOSE_PROJECT&gt;.env</c>.
    /// Falls back to legacy <c>$WORKSPACE_ROOT/parser-pipeline.env</c> when it matches this project.
    /// </summary>
    public static string? ResolveEnvFilePath()
    {
        var ws = DepExtractPathRules.TryGetWorkspaceRoot();
        if (ws is null) return null;
        var project = ResolveComposeProject();
        var canonical = CanonicalEnvFilePath(ws, project);
        if (File.Exists(canonical)) return canonical;

        var legacy = Path.Combine(ws, "parser-pipeline.env");
        if (File.Exists(legacy))
        {
            var legacyProject = TryReadEnvKey(legacy, "COMPOSE_PROJECT");
            if (string.IsNullOrWhiteSpace(legacyProject)
                || string.Equals(legacyProject, project, StringComparison.OrdinalIgnoreCase))
                return legacy;
        }

        return canonical;
    }

    /// <summary>Always the per-project path (created on write).</summary>
    public static string? ResolveCanonicalEnvFilePath()
    {
        var ws = DepExtractPathRules.TryGetWorkspaceRoot();
        if (ws is null) return null;
        return CanonicalEnvFilePath(ws, ResolveComposeProject());
    }

    static string CanonicalEnvFilePath(string workspaceRoot, string project)
    {
        var safe = string.Join("_", project.Split(
            Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safe)) safe = "nexo-parser-pipeline";
        var dir = Path.Combine(workspaceRoot, ".parser-pipeline");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, safe + ".env");
    }

    static string? TryReadEnvKey(string envPath, string key)
    {
        try
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith('#')) continue;
                var eq = t.IndexOf('=');
                if (eq <= 0) continue;
                if (!string.Equals(t[..eq], key, StringComparison.OrdinalIgnoreCase)) continue;
                return t[(eq + 1)..].Trim().Trim('"');
            }
        }
        catch
        {
            /* ignore */
        }
        return null;
    }

    public static PortSettings Load()
    {
        var path = ResolveSettingsPath();
        PortSettings? file = null;
        if (File.Exists(path))
        {
            try
            {
                file = JsonSerializer.Deserialize<PortSettings>(File.ReadAllText(path), JsonOpts);
            }
            catch
            {
                /* ignore corrupt */
            }
        }

        // Compose/env wins over a previously saved ports.json so setup scripts stay authoritative.
        var parser = ParsePort(Environment.GetEnvironmentVariable("EVTX_HOST_PORT"), 0);
        if (parser == 0)
            parser = TryPort(file?.ParserHostPort) ?? DefaultParserHostPort;

        var agent = ParsePort(
            Environment.GetEnvironmentVariable("AGENT_PORT")
                ?? Environment.GetEnvironmentVariable("GUI_PORT"),
            0);
        if (agent == 0)
            agent = TryPort(file?.AgentHostPort) ?? DefaultAgentHostPort;

        return new PortSettings(parser, agent);
    }

    public static PortSettings Save(int parserHostPort, int agentHostPort)
    {
        var settings = new PortSettings(ClampPort(parserHostPort), ClampPort(agentHostPort));
        var path = ResolveSettingsPath();
        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOpts));
        WriteEnvFile(settings);
        return settings;
    }

    public static void UpsertEnvVar(string key, string value)
    {
        var envPath = ResolveEnvFilePath();
        if (envPath is null) return;

        var ports = Load();
        WriteEnvFile(ports, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [key] = value,
        });
    }

    public static string? TryGetEnvVar(string key)
    {
        var envPath = ResolveEnvFilePath();
        if (envPath is null || !File.Exists(envPath)) return null;
        foreach (var line in File.ReadAllLines(envPath))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith('#')) continue;
            var eq = t.IndexOf('=');
            if (eq <= 0) continue;
            if (!string.Equals(t[..eq], key, StringComparison.OrdinalIgnoreCase)) continue;
            return t[(eq + 1)..].Trim().Trim('"');
        }
        return null;
    }

    static void WriteEnvFile(PortSettings settings, IDictionary<string, string>? extra = null)
    {
        var envPath = ResolveCanonicalEnvFilePath() ?? ResolveEnvFilePath();
        if (envPath is null) return;

        var project = ResolveComposeProject();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EVTX_HOST_PORT"] = settings.ParserHostPort.ToString(),
            ["AGENT_PORT"] = settings.AgentHostPort.ToString(),
            ["GUI_PORT"] = settings.AgentHostPort.ToString(),
            ["COMPOSE_PROJECT"] = project,
        };

        // Prefer live process env for portable path bindings (survives username changes).
        // Do NOT pull EVTX_IMAGE from process env here — compose agent containers often
        // still have the image they were started with, which would clobber a later
        // UpsertEnvVar("EVTX_IMAGE", "evtx:custom") on Save()/recreate.
        foreach (var key in new[]
                 {
                     "WORKSPACE_ROOT", "POCO_CONTEXT", "NEXO_ROOT", "PARSER_PIPELINE_COMPOSE",
                 })
        {
            var fromEnv = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                map[key] = fromEnv;
        }

        // Merge from existing canonical or readable env (gaps only).
        foreach (var existing in new[] { envPath, ResolveEnvFilePath() }.Where(p => p is not null).Distinct())
        {
            if (!File.Exists(existing!)) continue;
            foreach (var line in File.ReadAllLines(existing!))
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith('#')) continue;
                var eq = t.IndexOf('=');
                if (eq <= 0) continue;
                var k = t[..eq];
                var v = t[(eq + 1)..];
                if (!map.ContainsKey(k))
                    map[k] = v;
            }
        }

        if (extra is not null)
        {
            foreach (var kv in extra)
                map[kv.Key] = kv.Value;
        }

        // Identity keys always match this agent instance.
        map["COMPOSE_PROJECT"] = project;

        var preferred = new[]
        {
            "EVTX_HOST_PORT", "AGENT_PORT", "GUI_PORT", "EVTX_IMAGE",
            "COMPOSE_PROJECT", "WORKSPACE_ROOT", "POCO_CONTEXT", "NEXO_ROOT",
            "PARSER_PIPELINE_COMPOSE",
        };

        var sb = new StringBuilder();
        sb.AppendLine("# Generated by DepExtract agent / setup-parser-pipeline.sh");
        sb.AppendLine($"# Project: {project}");
        sb.AppendLine($"#   docker compose -p {project} -f docker-compose.parser-pipeline.yml --env-file {envPath} up -d");
        foreach (var key in preferred)
        {
            if (map.TryGetValue(key, out var val))
                sb.AppendLine($"{key}={val}");
        }
        foreach (var kv in map.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (preferred.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                continue;
            sb.AppendLine($"{kv.Key}={kv.Value}");
        }

        var dir = Path.GetDirectoryName(envPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(envPath, sb.ToString());

        // Active-stack pointer for older scripts that still open parser-pipeline.env
        var ws = DepExtractPathRules.TryGetWorkspaceRoot();
        if (ws is not null)
        {
            try
            {
                File.Copy(envPath, Path.Combine(ws, "parser-pipeline.env"), overwrite: true);
            }
            catch
            {
                /* best-effort */
            }
        }
    }

    public static string ParserUiUrl(PortSettings? settings = null)
    {
        var s = settings ?? Load();
        return $"http://127.0.0.1:{s.ParserHostPort}/";
    }

    /// <summary>
    /// Base URL the agent process should use to reach the parser (health/extract smoke).
    /// Inside compose, loopback is the agent container — use the service DNS name instead.
    /// </summary>
    public static string ParserReachableBaseUrl(PortSettings? settings = null)
    {
        var overrideUrl = Environment.GetEnvironmentVariable("EVTX_INTERNAL_URL");
        if (!string.IsNullOrWhiteSpace(overrideUrl))
            return overrideUrl.TrimEnd('/');

        if (File.Exists("/.dockerenv"))
            return "http://evtx:8080";

        return ParserUiUrl(settings).TrimEnd('/');
    }

    public static string AgentUiUrl(PortSettings? settings = null)
    {
        var s = settings ?? Load();
        return $"http://127.0.0.1:{s.AgentHostPort}/";
    }

    public static string RestartHint(PortSettings settings)
    {
        var env = ResolveEnvFilePath();
        if (env is not null && File.Exists(env))
        {
            return
                $"Ports saved. Recreate containers with:\n" +
                $"  export EVTX_HOST_PORT={settings.ParserHostPort} AGENT_PORT={settings.AgentHostPort}\n" +
                $"  docker compose -f docker-compose.parser-pipeline.yml --env-file {env} up -d --force-recreate\n" +
                $"Or use the agent UI Recreate button / POST /api/ports/recreate.\n" +
                $"Then open {AgentUiUrl(settings)} (agent) and {ParserUiUrl(settings)} (parser).";
        }

        return
            $"Ports saved. Recreate containers with:\n" +
            $"  EVTX_HOST_PORT={settings.ParserHostPort} AGENT_PORT={settings.AgentHostPort} \\\n" +
            $"    docker compose -f docker-compose.parser-pipeline.yml up -d --force-recreate\n" +
            $"Then open {AgentUiUrl(settings)} (agent) and {ParserUiUrl(settings)} (parser).";
    }

    static int ParsePort(string? raw, int fallback)
        => int.TryParse(raw, out var n) && n is >= 1 and <= 65535 ? n : fallback;

    static int? TryPort(int? port)
        => port is >= 1 and <= 65535 ? port : null;

    static int ClampPort(int port)
    {
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be 1–65535");
        return port;
    }
}

public sealed record PortSettings(int ParserHostPort, int AgentHostPort);
