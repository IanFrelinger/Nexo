using System.Diagnostics;
using System.Text;
using Nexo.Bricks.DepExtract;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>Host docker compose helpers for the parser pipeline (recreate / env upsert).</summary>
public static class ComposePipeline
{
    /// <summary>Services the agent is allowed to recreate via docker.sock.</summary>
    public static readonly HashSet<string> AllowedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "evtx",
        "dep-extract",
        "dep-extract-agent",
        "ollama",
        "ollama-pull",
    };

    public static string ProjectName => PortSettingsStore.ResolveComposeProject();

    /// <summary>
    /// Fail fast when env file / host ports belong to a different compose project
    /// (avoids recreate binding the wrong stack's ports).
    /// </summary>
    public static void EnsureStackIdentity()
    {
        var project = ProjectName;
        var envFile = PortSettingsStore.ResolveEnvFilePath();
        if (string.IsNullOrWhiteSpace(envFile) || !File.Exists(envFile))
        {
            throw new InvalidOperationException(
                $"compose_env_missing: no env file for COMPOSE_PROJECT={project}. " +
                "Expected $WORKSPACE_ROOT/.parser-pipeline/<project>.env — run setup-parser-pipeline.sh / pp.");
        }

        var fileProject = PortSettingsStore.TryGetEnvVar("COMPOSE_PROJECT");
        if (!string.IsNullOrWhiteSpace(fileProject)
            && !string.Equals(fileProject, project, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"compose_project_mismatch: agent COMPOSE_PROJECT={project} but env file has " +
                $"COMPOSE_PROJECT={fileProject} ({envFile}). Use the per-project env under " +
                "$WORKSPACE_ROOT/.parser-pipeline/<project>.env or re-run setup with matching COMPOSE_PROJECT.");
        }

        var ports = PortSettingsStore.Load();
        var owner = FindContainerPublishingPort(ports.ParserHostPort);
        if (owner is not null && !ContainerBelongsToProject(owner, project))
        {
            throw new InvalidOperationException(
                $"compose_port_conflict: host :{ports.ParserHostPort} is published by '{owner}', " +
                $"not compose project '{project}'. Stop the other stack or pick free ports for this project.");
        }
    }

    /// <summary>Best-effort identity check for /api/status (never throws).</summary>
    public static (bool Ok, string Detail) TryCheckStackIdentity()
    {
        try
        {
            EnsureStackIdentity();
            return (true, $"ok project={ProjectName} env={PortSettingsStore.ResolveEnvFilePath()}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    static string? FindContainerPublishingPort(int hostPort)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("ps");
            psi.ArgumentList.Add("--format");
            psi.ArgumentList.Add("{{.Names}}\t{{.Ports}}");
            using var p = Process.Start(psi);
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10_000);
            if (p.ExitCode != 0) return null;

            var needle = $":{hostPort}->";
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                var name = line[..tab];
                var ports = line[(tab + 1)..];
                if (ports.Contains(needle, StringComparison.Ordinal))
                    return name;
            }
        }
        catch
        {
            /* ignore — preflight is best-effort when docker is unavailable */
        }
        return null;
    }

    static bool ContainerBelongsToProject(string containerName, string project)
    {
        // Compose names: {project}-{service}-{replica}
        if (containerName.StartsWith(project + "-", StringComparison.OrdinalIgnoreCase))
            return true;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("inspect");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("{{index .Config.Labels \"com.docker.compose.project\"}}");
            psi.ArgumentList.Add(containerName);
            using var p = Process.Start(psi);
            if (p is null) return false;
            var label = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(10_000);
            return string.Equals(label, project, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string? TryFindComposeFile()
    {
        var explicitPath = Environment.GetEnvironmentVariable("PARSER_PIPELINE_COMPOSE");
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return Path.GetFullPath(explicitPath);

        var nexo = Environment.GetEnvironmentVariable("NEXO_ROOT");
        if (!string.IsNullOrWhiteSpace(nexo))
        {
            var p = Path.Combine(nexo, "docker-compose.parser-pipeline.yml");
            if (File.Exists(p)) return Path.GetFullPath(p);
        }

        var ws = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        if (!string.IsNullOrWhiteSpace(ws))
        {
            foreach (var rel in new[] { "nexo/docker-compose.parser-pipeline.yml", "docker-compose.parser-pipeline.yml" })
            {
                var p = Path.Combine(ws, rel);
                if (File.Exists(p)) return Path.GetFullPath(p);
            }
        }

        return null;
    }

    public static ComposePlan PlanRecreate(IReadOnlyList<string>? services, bool withExtractAgent)
    {
        EnsureStackIdentity();

        var composeFile = TryFindComposeFile()
            ?? throw new InvalidOperationException(
                "docker-compose.parser-pipeline.yml not found. Set NEXO_ROOT or PARSER_PIPELINE_COMPOSE.");
        var envFile = PortSettingsStore.ResolveEnvFilePath();
        if (envFile is null || !File.Exists(envFile))
            throw new InvalidOperationException(
                "compose env missing — Apply ports or run setup-parser-pipeline.sh / pp first.");

        var resolved = ResolveServices(services, withExtractAgent);
        var needsProfile = withExtractAgent
            || resolved.Any(s => s is "dep-extract" or "dep-extract-agent" or "ollama" or "ollama-pull");
        var args = BuildArgs(ProjectName, composeFile, envFile, needsProfile, resolved);
        var cmd = "docker compose " + string.Join(" ", args.Select(QuoteArg));
        var missing = new List<string>();
        if (!File.Exists(composeFile)) missing.Add("composeFile");
        if (!File.Exists(envFile)) missing.Add("envFile");
        return new ComposePlan(ProjectName, composeFile, envFile, resolved.ToArray(), cmd, missing.Count == 0, missing.ToArray());
    }

    public static async Task<ComposeRunResult> RecreateAsync(
        IReadOnlyList<string>? services,
        bool withExtractAgent,
        CancellationToken ct,
        bool dryRun = false,
        bool waitHealthy = true,
        int? previousAgentPort = null,
        Action<string>? onLog = null)
    {
        var ports = PortSettingsStore.Load();
        PortSettingsStore.Save(ports.ParserHostPort, ports.AgentHostPort);
        var plan = PlanRecreate(services, withExtractAgent);
        if (!plan.PathsOk)
            throw new InvalidOperationException("Compose paths invalid: " + string.Join(", ", plan.Missing));

        onLog?.Invoke(plan.Command);

        if (dryRun)
        {
            return new ComposeRunResult(
                true,
                "dry-run: " + plan.Command,
                plan.Project,
                plan.ComposeFile,
                plan.EnvFile,
                plan.Services,
                plan.Command,
                null,
                PortSettingsStore.ParserUiUrl(ports),
                PortSettingsStore.AgentUiUrl(ports),
                previousAgentPort is int prev && prev != ports.AgentHostPort);
        }

        async Task<ComposeRunResult> RunAsync(string fileName, bool useComposeSubcommand)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            // Compose prefers process env over --env-file. Seed from the per-project env
            // so EVTX_IMAGE=evtx:custom (etc.) wins over a stale agent container env.
            foreach (var kv in ReadEnvFile(plan.EnvFile))
                psi.Environment[kv.Key] = kv.Value;
            // Never let a stale env file redirect -p to another stack.
            psi.Environment["COMPOSE_PROJECT"] = plan.Project;
            psi.Environment["COMPOSE_PROJECT_NAME"] = plan.Project;
            if (useComposeSubcommand)
                psi.ArgumentList.Add("compose");
            var needsProfile = withExtractAgent
                || plan.Services.Any(s => s is "dep-extract" or "dep-extract-agent" or "ollama" or "ollama-pull");
            foreach (var a in BuildArgs(plan.Project, plan.ComposeFile, plan.EnvFile, needsProfile, plan.Services))
                psi.ArgumentList.Add(a);

            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException($"failed to start {fileName}");
            var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = p.StandardError.ReadToEndAsync(ct);
            // Compose recreate talks to the daemon (cannot use --network=none on the
            // CLI process); still enforce a wall-clock timeout via ArgumentList-based invoke.
            var composeTimeout = int.TryParse(
                Environment.GetEnvironmentVariable("NEXO_COMPOSE_RECREATE_TIMEOUT_SEC"), out var cts) && cts > 0
                ? cts
                : DockerHardening.BuildTimeoutSeconds;
            int exitCode;
            try
            {
                exitCode = await DockerHardening.WaitForExitWithTimeoutAsync(p, composeTimeout, ct);
            }
            catch (TimeoutException tex)
            {
                return new ComposeRunResult(false, tex.Message, plan.Project, plan.ComposeFile, plan.EnvFile,
                    plan.Services, plan.Command, null, null, null, false);
            }
            var log = ((await stdoutTask) + "\n" + (await stderrTask)).Trim();
            onLog?.Invoke(log);
            return new ComposeRunResult(exitCode == 0, log, plan.Project, plan.ComposeFile, plan.EnvFile,
                plan.Services, plan.Command, null, null, null, false);
        }

        var primary = await RunAsync("docker", useComposeSubcommand: true);
        ComposeRunResult result = primary;
        if (!primary.Ok
            && (primary.Log.Contains("unknown shorthand flag", StringComparison.OrdinalIgnoreCase)
                || primary.Log.Contains("is not a docker command", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var fallback = await RunAsync("docker-compose", useComposeSubcommand: false);
                result = fallback.Ok
                    ? fallback
                    : primary with { Log = primary.Log + "\n---\n" + fallback.Log };
            }
            catch (Exception ex)
            {
                result = primary with
                {
                    Ok = false,
                    Log = primary.Log + "\n---\ndocker-compose fallback failed: " + ex.Message,
                };
            }
        }

        if (!result.Ok)
            return result;

        string? healthLog = null;
        var reloadAgent = previousAgentPort is int prevPort && prevPort != ports.AgentHostPort;
        if (waitHealthy)
        {
            healthLog = await WaitHealthyAsync(
                ports,
                plan.Services.Contains("evtx", StringComparer.OrdinalIgnoreCase),
                plan.Services.Any(s => s.Contains("agent", StringComparison.OrdinalIgnoreCase)) || withExtractAgent,
                ct,
                onLog);
        }

        return result with
        {
            HealthLog = healthLog,
            ReloadAgentSuggested = reloadAgent,
            AgentUiUrl = PortSettingsStore.AgentUiUrl(ports),
            ParserUiUrl = PortSettingsStore.ParserUiUrl(ports),
        };
    }

    public static async Task<string> WaitHealthyAsync(
        PortSettings ports,
        bool waitParser,
        bool waitAgent,
        CancellationToken ct,
        Action<string>? onLog = null)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var sb = new StringBuilder();
        // Wall-clock bound for post-recreate smoke/health (compose itself must keep host
        // networking so /health is reachable — --network=none is not feasible here).
        var healthySec = int.TryParse(
            Environment.GetEnvironmentVariable("NEXO_COMPOSE_HEALTH_TIMEOUT_SEC"), out var hs) && hs > 0
            ? hs
            : DockerHardening.RunTimeoutSeconds;
        if (healthySec < 30) healthySec = 30;
        var deadline = DateTime.UtcNow.AddSeconds(healthySec);

        if (waitParser)
        {
            var url = PortSettingsStore.ParserReachableBaseUrl(ports) + "/health";
            var ok = false;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var resp = await http.GetAsync(url, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        onLog?.Invoke($"parser healthy: {url}");
                        sb.AppendLine($"parser healthy: {url}");
                        ok = true;
                        break;
                    }
                }
                catch { /* retry */ }
                await Task.Delay(500, ct);
            }
            if (!ok)
                sb.AppendLine($"parser NOT healthy within timeout: {url}");
        }

        if (waitAgent)
        {
            var url = PortSettingsStore.AgentUiUrl(ports).TrimEnd('/') + "/api/status";
            var ok = false;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var resp = await http.GetAsync(url, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        onLog?.Invoke($"agent healthy: {url}");
                        sb.AppendLine($"agent healthy: {url}");
                        ok = true;
                        break;
                    }
                }
                catch { /* retry */ }
                await Task.Delay(500, ct);
            }
            if (!ok)
                sb.AppendLine($"agent NOT healthy within timeout: {url}");
        }

        return sb.ToString().Trim();
    }

    static List<string> ResolveServices(IReadOnlyList<string>? services, bool withExtractAgent)
    {
        List<string> resolved;
        if (services is { Count: > 0 })
            resolved = services.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        else if (withExtractAgent)
            resolved = new List<string> { "evtx", "dep-extract", "dep-extract-agent" };
        else
            resolved = new List<string> { "evtx" };

        var bad = resolved.Where(s => !AllowedServices.Contains(s)).ToArray();
        if (bad.Length > 0)
            throw new InvalidOperationException(
                "Service not allowlisted for recreate: " + string.Join(", ", bad)
                + ". Allowed: " + string.Join(", ", AllowedServices.OrderBy(x => x)));
        return resolved.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    static List<string> BuildArgs(
        string project,
        string composeFile,
        string envFile,
        bool withExtractAgentProfile,
        IReadOnlyList<string> services)
    {
        var args = new List<string>
        {
            "-p", project,
            "-f", composeFile,
            "--env-file", envFile,
        };
        if (withExtractAgentProfile)
        {
            args.Add("--profile");
            args.Add("extract-agent");
        }
        args.Add("up");
        args.Add("-d");
        args.Add("--force-recreate");
        args.AddRange(services);
        return args;
    }

    static string QuoteArg(string a)
        => a.Any(char.IsWhiteSpace) ? "\"" + a.Replace("\"", "\\\"") + "\"" : a;

    static IEnumerable<KeyValuePair<string, string>> ReadEnvFile(string envFile)
    {
        if (string.IsNullOrWhiteSpace(envFile) || !File.Exists(envFile))
            yield break;
        foreach (var line in File.ReadLines(envFile))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith('#')) continue;
            var eq = t.IndexOf('=');
            if (eq <= 0) continue;
            yield return new KeyValuePair<string, string>(t[..eq], t[(eq + 1)..].Trim().Trim('"'));
        }
    }
}

public sealed record ComposePlan(
    string Project,
    string ComposeFile,
    string EnvFile,
    string[] Services,
    string Command,
    bool PathsOk,
    string[] Missing);

public sealed record ComposeRunResult(
    bool Ok,
    string Log,
    string Project,
    string ComposeFile,
    string EnvFile,
    string[] Services,
    string Command,
    string? HealthLog,
    string? ParserUiUrl,
    string? AgentUiUrl,
    bool ReloadAgentSuggested);
