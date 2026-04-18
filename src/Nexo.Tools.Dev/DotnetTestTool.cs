using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

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
    {"type":"object","required":["root"],"properties":{"root":{"type":"string"}}}
    """);

    private sealed record Args(string root);

    /// <summary>Shared implementation for <see cref="DotnetTestTool"/>, <see cref="ForgeTestTool"/>, and operator CLIs.</summary>
    public static Task<(int exitCode, string stdout, string stderr, bool timedOut)> RunTrxTestsNoBuildAsync(
        string workingDirectory,
        CancellationToken ct = default) =>
        DotnetRunner.RunAsync(workingDirectory, TrxNoBuildArguments, TimeSpan.FromMinutes(20), ct);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var (code, stdout, stderr, timedOut) = await RunTrxTestsNoBuildAsync(args.root, ct).ConfigureAwait(false);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"test:exit={code}");
        if (timedOut) delta.AddLog("test:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("test:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }
}
