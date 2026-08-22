using Ashlar.Abstractions;
using Ashlar.Tools.Dev.Deltas;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Tool for building .NET projects using dotnet CLI.
/// 
/// Executes `dotnet build -c Release` in the specified root directory.
/// Returns build exit code, stdout, and stderr in the tool result.
/// 
/// Implements ITool for use with agent tool execution.
/// </summary>
public sealed class DotnetBuildTool : ITool
{
    public string Id => "dotnet.build";
    public ToolSchema Schema => new(Id, "Run dotnet build -c Release", """
    {"type":"object","required":["root"],"properties":{"root":{"type":"string"}}}
    """);

    private sealed record Args(string root);

    /// <summary>
    /// Shared implementation for <see cref="DotnetBuildTool"/>, <see cref="ForgeBuildTool"/>,
    /// and operator CLIs that need the same <c>dotnet build -c Release</c> contract.
    /// </summary>
    public static Task<(int exitCode, string stdout, string stderr, bool timedOut)> RunReleaseBuildAsync(
        string workingDirectory,
        CancellationToken ct = default)
    {
        var arguments = ResolveBuildArguments(workingDirectory);
        return DotnetRunner.RunAsync(workingDirectory, arguments, TimeSpan.FromMinutes(10), ct);
    }

    internal static string ResolveBuildArguments(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
        {
            return "build -c Release";
        }

        var preferredTargets = new[]
        {
            "Ashlar.LocalDevCore.slnf",
            "Ashlar.Core.slnf"
        };

        foreach (var target in preferredTargets)
        {
            if (File.Exists(Path.Combine(workingDirectory, target)))
            {
                return $"build {Quote(target)} -c Release";
            }
        }

        var buildFiles = Directory.EnumerateFiles(workingDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path =>
                path.EndsWith(".slnf", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return buildFiles.Length == 1
            ? $"build {Quote(Path.GetFileName(buildFiles[0]))} -c Release"
            : "build -c Release";
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var (code, stdout, stderr, timedOut) = await RunReleaseBuildAsync(args.root, ct).ConfigureAwait(false);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"build:exit={code}");
        if (timedOut) delta.AddLog("build:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("build:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }
}
