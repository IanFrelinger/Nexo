using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Playtesting;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Standard background-agent adapter that executes a bounded first-pass Weapon
/// Lab session through <see cref="UnityWeaponLabGameRunner"/>.
/// </summary>
public sealed class PlaytestRunRunnerAdapter : IPlaytestRunRunner
{
    private readonly UnityWeaponLabGameRunner _runner;
    private readonly ILogger<PlaytestRunRunnerAdapter> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PlaytestRunRunnerAdapter(
        UnityWeaponLabGameRunner runner,
        ILogger<PlaytestRunRunnerAdapter> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    public async Task<PlaytestRunResult> RunAsync(
        BackgroundAgentConfig config,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var actions = 0;
        try
        {
            var buildPath = Parameter(
                config,
                "BuildPath",
                "../NexoBattleRoyale/Builds/WeaponLab/NexoBRWeaponLab.app");
            buildPath = Path.GetFullPath(buildPath);
            var artifactRoot = Path.GetFullPath(Parameter(
                config,
                "ArtifactRoot",
                ".nexo/playtest/br-weapon-lab"));
            Directory.CreateDirectory(artifactRoot);
            Environment.SetEnvironmentVariable(
                "NEXO_BR_PLAYTEST_ARTIFACT_ROOT",
                artifactRoot);

            await _runner.StartAsync(buildPath, cancellationToken)
                .ConfigureAwait(false);
            var initial = await _runner.GetGameStateAsync(cancellationToken)
                .ConfigureAwait(false);

            await _runner.ExecuteActionAsync(
                "move",
                new Dictionary<string, object>
                {
                    ["x"] = 0,
                    ["y"] = 127,
                    ["duration_ms"] = 400
                },
                cancellationToken).ConfigureAwait(false);
            actions++;
            await _runner.ExecuteActionAsync(
                "interact",
                new Dictionary<string, object>
                {
                    ["duration_ms"] = 300
                },
                cancellationToken).ConfigureAwait(false);
            actions++;
            await _runner.ExecuteActionAsync(
                "fire",
                new Dictionary<string, object>
                {
                    ["duration_ms"] = 300,
                    ["ads"] = true
                },
                cancellationToken).ConfigureAwait(false);
            actions++;
            await _runner.ExecuteActionAsync(
                "reload",
                new Dictionary<string, object>
                {
                    ["duration_ms"] = 300
                },
                cancellationToken).ConfigureAwait(false);
            actions++;
            await _runner.ExecuteActionAsync(
                "wait",
                new Dictionary<string, object>
                {
                    ["duration_ms"] = 1800
                },
                cancellationToken).ConfigureAwait(false);
            actions++;
            const string captureName = "standard-background-agent.png";
            await _runner.ExecuteActionAsync(
                "capture",
                new Dictionary<string, object>
                {
                    ["path"] = captureName
                },
                cancellationToken).ConfigureAwait(false);
            actions++;

            var final = await _runner.GetGameStateAsync(cancellationToken)
                .ConfigureAwait(false);
            var reportPath = Path.Combine(
                artifactRoot,
                "standard-background-agent-report.json");
            var success =
                final.PlayerHealth > 0 &&
                !string.IsNullOrWhiteSpace(final.EquippedWeapon) &&
                initial.PlayerPosition != final.PlayerPosition;
            var report = new
            {
                generatedAt = DateTimeOffset.UtcNow,
                agentId = config.Id,
                success,
                initial,
                final,
                actions,
                screenshot = Path.Combine(artifactRoot, captureName)
            };
            await File.WriteAllTextAsync(
                reportPath,
                JsonSerializer.Serialize(
                    report,
                    new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken).ConfigureAwait(false);

            return new PlaytestRunResult(
                success,
                success
                    ? $"Weapon Lab first pass completed ({actions} actions)."
                    : "Weapon Lab first pass did not satisfy state checks.",
                reportPath,
                actions,
                new Dictionary<string, string>
                {
                    ["initial_weapon"] = initial.EquippedWeapon ?? "",
                    ["final_weapon"] = final.EquippedWeapon ?? "",
                    ["health"] = final.PlayerHealth.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Playtest background agent {AgentId} failed",
                config.Id);
            return new PlaytestRunResult(
                false,
                exception.Message,
                null,
                actions);
        }
        finally
        {
            try
            {
                await _runner.StopAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Ignoring playtest shutdown failure");
            }
            _gate.Release();
        }
    }

    private static string Parameter(
        BackgroundAgentConfig config,
        string name,
        string fallback)
    {
        if (config.Parameters is null ||
            !config.Parameters.TryGetValue(name, out var value) ||
            value is null)
            return fallback;
        return value is JsonElement json
            ? json.GetString() ?? fallback
            : value.ToString() ?? fallback;
    }
}
