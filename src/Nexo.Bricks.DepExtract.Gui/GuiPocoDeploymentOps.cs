using Nexo.Bricks.DepExtract.Profile;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// <see cref="IPocoDeploymentOps"/> backed by <see cref="ComposePipeline"/> /
/// docker CLI — Gui-only concrete wiring for <see cref="PocoInstallTarget"/>.
/// </summary>
public sealed class GuiPocoDeploymentOps : IPocoDeploymentOps
{
    /// <inheritdoc />
    public void EnsureStackIdentity() => ComposePipeline.EnsureStackIdentity();

    /// <inheritdoc />
    public async Task RetagImageAsync(string fromTag, string toTag, CancellationToken cancellationToken)
    {
        // Best-effort — same semantics as InstallExecutor.DockerRetagAsync.
        try
        {
            var inspect = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            inspect.ArgumentList.Add("image");
            inspect.ArgumentList.Add("inspect");
            inspect.ArgumentList.Add(fromTag);
            using var p = System.Diagnostics.Process.Start(inspect);
            if (p is null) return;
            await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (p.ExitCode != 0) return;

            var tag = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            tag.ArgumentList.Add("tag");
            tag.ArgumentList.Add(fromTag);
            tag.ArgumentList.Add(toTag);
            using var t = System.Diagnostics.Process.Start(tag);
            if (t is null) return;
            await t.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // best-effort
        }
    }

    /// <inheritdoc />
    public async Task<(bool Ok, string Log)> BuildCustomImageAsync(
        string pocoContext,
        string imageTag,
        CancellationToken cancellationToken)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = pocoContext,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["DOCKER_BUILDKIT"] = "1";
        psi.ArgumentList.Add("build");
        DockerHardening.AddBuildIsolationArgs(psi);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("Dockerfile.custom");
        psi.ArgumentList.Add("-t");
        psi.ArgumentList.Add(imageTag);
        psi.ArgumentList.Add(".");
        try
        {
            using var p = System.Diagnostics.Process.Start(psi)
                ?? throw new InvalidOperationException("failed to start docker build");
            var stdoutTask = p.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = p.StandardError.ReadToEndAsync(cancellationToken);
            var exit = await DockerHardening.WaitForExitWithTimeoutAsync(
                p, DockerHardening.BuildTimeoutSeconds, cancellationToken).ConfigureAwait(false);
            var log = ((await stdoutTask.ConfigureAwait(false)) + "\n" + (await stderrTask.ConfigureAwait(false))).Trim();
            return (exit == 0, log);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<(bool Ok, string Log)> RecreateServiceAsync(
        string service,
        bool waitHealthy,
        CancellationToken cancellationToken)
    {
        if (!PocoInstallTarget.AllowedServices.Contains(service))
        {
            return (false,
                $"service '{service}' is not allowed. Allowed: "
                + string.Join(", ", PocoInstallTarget.AllowedServices.OrderBy(x => x)));
        }

        var result = await ComposePipeline.RecreateAsync(
            new[] { service },
            withExtractAgent: false,
            cancellationToken,
            dryRun: false,
            waitHealthy: waitHealthy,
            previousAgentPort: null,
            onLog: null).ConfigureAwait(false);
        var log = string.Join("\n", new[] { result.Log, result.HealthLog }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return (result.Ok, log);
    }

    /// <inheritdoc />
    public Task<(bool Ok, string Detail)> SmokeAsync(string? extractFile, CancellationToken cancellationToken) =>
        // Reuse InstallExecutor smoke by delegating through a minimal install is heavy;
        // health-only check here — AdaptInstallLoop still uses InstallExecutor.RunSyncAsync for smoke today.
        SmokeHealthAsync(cancellationToken);

    /// <inheritdoc />
    public void UpsertEnvVar(string key, string value) => PortSettingsStore.UpsertEnvVar(key, value);

    private static async Task<(bool Ok, string Detail)> SmokeHealthAsync(CancellationToken ct)
    {
        var ports = PortSettingsStore.Load();
        var baseUrl = PortSettingsStore.ParserReachableBaseUrl(ports);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var health = await http.GetAsync($"{baseUrl}/health", ct).ConfigureAwait(false);
            return health.IsSuccessStatusCode
                ? (true, $"health ok via {baseUrl}")
                : (false, $"health HTTP {(int)health.StatusCode} via {baseUrl}");
        }
        catch (Exception ex)
        {
            return (false, $"{ex.Message} (via {baseUrl})");
        }
    }
}
