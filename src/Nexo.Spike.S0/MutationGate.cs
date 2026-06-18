using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S0;

public sealed record MutationGateResult(
    bool Passed,
    double MutationScore,
    IReadOnlyList<string> SurvivingMutants,
    string RawOutput);

/// <summary>
/// Runs Stryker.NET and enforces a mutation score threshold at VERIFY.
/// </summary>
public sealed class MutationGate
{
    private static readonly Regex ScoreRegex = new(
        @"mutation score (?:of |is )(\d+(?:\.\d+)?)\s*%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<MutationGateResult> RunAsync(
        string workspaceRoot,
        double thresholdPercent,
        CancellationToken ct = default)
    {
        var configPath = Path.Combine(workspaceRoot, "stryker-config.json");
        if (!File.Exists(configPath))
        {
            return new MutationGateResult(
                false,
                0,
                ["stryker-config.json missing"],
                "stryker-config.json not found");
        }

        var outputDir = Path.Combine(workspaceRoot, "mutation-reports");
        Directory.CreateDirectory(outputDir);

        var psi = new ProcessStartInfo("dotnet", $"stryker --config-file \"{configPath}\" --output \"{outputDir}\"")
        {
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        var combined = $"{stdout}\n{stderr}";
        var score = ParseScore(combined, outputDir);
        var survivors = await ParseSurvivorsAsync(outputDir, ct).ConfigureAwait(false);
        var passed = score >= thresholdPercent;

        return new MutationGateResult(passed, score, survivors, combined);
    }

    private static double ParseScore(string output, string outputDir)
    {
        var match = ScoreRegex.Match(output);
        if (match.Success && double.TryParse(match.Groups[1].Value, out var parsed))
            return parsed;

        var jsonReports = Directory.Exists(outputDir)
            ? Directory.GetFiles(outputDir, "mutation-report.json", SearchOption.AllDirectories)
            : [];
        foreach (var report in jsonReports)
        {
            try
            {
                using var stream = File.OpenRead(report);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("thresholds", out var thresholds) &&
                    thresholds.TryGetProperty("score", out var scoreEl) &&
                    scoreEl.TryGetDouble(out var score))
                {
                    return score;
                }
            }
            catch
            {
                // try next report
            }
        }

        return 0;
    }

    private static async Task<IReadOnlyList<string>> ParseSurvivorsAsync(string outputDir, CancellationToken ct)
    {
        var survivors = new List<string>();
        if (!Directory.Exists(outputDir))
            return survivors;

        foreach (var report in Directory.GetFiles(outputDir, "mutation-report.json", SearchOption.AllDirectories))
        {
            try
            {
                await using var stream = File.OpenRead(report);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                if (!doc.RootElement.TryGetProperty("files", out var files))
                    continue;

                foreach (var file in files.EnumerateObject())
                {
                    if (!file.Value.TryGetProperty("mutants", out var mutants))
                        continue;

                    foreach (var mutant in mutants.EnumerateArray())
                    {
                        if (mutant.TryGetProperty("status", out var status) &&
                            string.Equals(status.GetString(), "Survived", StringComparison.OrdinalIgnoreCase))
                        {
                            var id = mutant.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                            var replacement = mutant.TryGetProperty("replacement", out var repEl) ? repEl.GetString() : null;
                            survivors.Add($"{file.Name}:{id}:{replacement}");
                        }
                    }
                }
            }
            catch
            {
                // ignore malformed report
            }
        }

        return survivors;
    }
}
