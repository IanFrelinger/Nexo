using System.Globalization;
using System.Text.RegularExpressions;
using Ashlar.Abstractions;
using Ashlar.Tools.Dev.Deltas;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Tool for running .NET tests using dotnet CLI.
///
/// Executes `dotnet test -c Release --logger trx --no-build --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal`
/// in the specified root directory. Blame-hang safeguards prevent 6GB+ hang dumps; minimal verbosity reduces output buffering.
/// Returns test exit code, stdout, and stderr in the tool result.
///
/// Implements ITool for use with agent tool execution.
/// </summary>
public sealed class DotnetTestTool : ITool
{
    /// <summary>Arguments passed to <c>dotnet</c> for TRX test runs (must match <see cref="InvokeAsync"/>).</summary>
    /// <remarks><c>-c Release</c> matches <see cref="DotnetBuildTool"/> so <c>--no-build</c> test runs pick up the release output.</remarks>
    public const string TrxNoBuildArguments =
        "test -c Release --logger trx --no-build --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal";

    public string Id => "dotnet.test";
    public ToolSchema Schema => new(Id, "Run dotnet test -c Release --no-build --logger trx --blame-hang-timeout 60s --blame-hang-dump-type none", """
    {"type":"object","properties":{}}
    """);


    /// <summary>Shared implementation for <see cref="DotnetTestTool"/>, <see cref="ForgeTestTool"/>, and operator CLIs.</summary>
    public static Task<(int exitCode, string stdout, string stderr, bool timedOut)> RunTrxTestsNoBuildAsync(
        string workingDirectory,
        CancellationToken ct = default) =>
        DotnetRunner.RunAsync(workingDirectory, TrxNoBuildArguments, TimeSpan.FromMinutes(20), ct);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        // Working directory from the sandbox, not the model — see DotnetBuildTool.
        if (!ToolSandbox.TryResolveRoot(s, out var root, out var reason))
        {
            var rejected = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
            rejected.AddLog($"test:{reason}");
            return new ToolResult(rejected, new { ok = false, error = reason });
        }

        var (code, stdout, stderr, timedOut) = await RunTrxTestsNoBuildAsync(root, ct).ConfigureAwait(false);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"test:exit={code}");
        if (timedOut) delta.AddLog("test:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("test:stderr");
        return new ToolResult(delta, new { ok = Succeeded(code, timedOut, stdout, stderr), stdout, stderr });
    }

    /// <summary>
    /// Same class as <c>ashlar test local</c>: <c>dotnet test</c> exits 0 when no
    /// tests are available or a filter matches nothing.
    /// </summary>
    public static bool Succeeded(int exitCode, bool timedOut, string? stdout, string? stderr)
        => !timedOut && exitCode == 0 && HasExecutedTests(stdout, stderr);

    public static bool HasExecutedTests(string? stdout, string? stderr)
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
