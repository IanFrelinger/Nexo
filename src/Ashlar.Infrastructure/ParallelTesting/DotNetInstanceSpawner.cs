using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Ashlar.Core.Application.ParallelTesting.Models;
using Ashlar.Core.Application.ParallelTesting.Ports;

namespace Ashlar.Infrastructure.ParallelTesting;

/// <summary>
/// Spawns N dotnet test processes with different parameter sets.
/// </summary>
public sealed class DotNetInstanceSpawner : IInstanceSpawner
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TestInstance>> SpawnAsync(int count, IReadOnlyList<ParameterSet> paramSets, string solutionOrProjectPath, CancellationToken cancellationToken = default)
    {
        var results = new List<TestInstance>();
        var sets = paramSets.Count > 0 ? paramSets : new[] { new ParameterSet() };
        var toRun = Math.Min(count, sets.Count);
        for (var i = 0; i < toRun; i++)
        {
            var paramSet = sets[i];
            var filter = paramSet.Overrides.TryGetValue("filter", out var f) ? f : null;
            var sw = Stopwatch.StartNew();
            var (passed, output) = await RunDotNetTestAsync(solutionOrProjectPath, filter, cancellationToken).ConfigureAwait(false);
            sw.Stop();
            results.Add(new TestInstance
            {
                InstanceId = $"instance-{i}",
                ParameterSet = paramSet,
                Passed = passed,
                Output = output,
                Duration = sw.Elapsed,
            });
        }
        return results;
    }

    private static async Task<(bool Passed, string Output)> RunDotNetTestAsync(string path, string? filter, CancellationToken ct)
    {
        if (!File.Exists(path))
            return (false, $"Path not found: {path}");

        var args = $"test \"{path}\" --no-build --verbosity minimal";
        if (!string.IsNullOrWhiteSpace(filter))
            args += $" --filter \"{filter}\"";

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null)
                return (false, "Failed to start dotnet test");
            var stdout = await proc.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var stderr = await proc.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            var output = stdout + stderr;
            // Same class as ashlar test local / planner dotnet.test: `dotnet test --filter`
            // exits 0 when discovery matches nothing, so Passed used to be true.
            var executed = HasExecutedTests(output);
            var success = proc.ExitCode == 0 && executed;
            if (!success && proc.ExitCode == 0 && !executed)
                output += Environment.NewLine + "No tests matched the filter";
            return (success, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    internal static bool HasExecutedTests(string? stdout, string? stderr = null)
    {
        var output = string.Concat(stdout, "\n", stderr);
        if (output.Contains("No test is available", StringComparison.OrdinalIgnoreCase))
            return false;
        if (output.Contains("No test matches", StringComparison.OrdinalIgnoreCase))
            return false;

        var passed = Regex.Match(output, @"Passed:\s*(\d+)");
        if (!passed.Success)
            return false;
        return int.Parse(passed.Groups[1].Value, CultureInfo.InvariantCulture) >= 1;
    }
}
