using System.Diagnostics;
using System.Text.Json;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;

/// <summary>
/// Runs the framework's internal test runner via the CLI (`nexo test`) so the demo uses
/// the same aggregator/test-discovery mechanism as the rest of the system.
/// </summary>
public sealed class RunNexoTestCommand : ICommand<SelfExtendContext, SelfExtendContext>
{
    private readonly string _filter;

    public RunNexoTestCommand(string filter) => _filter = filter;

    public async ValueTask<SelfExtendContext> ExecuteAsync(SelfExtendContext input, CancellationToken ct)
    {
        var (exitCode, stdout, stderr) = await RunDotnetAsync(
            input.RepoRoot,
            $"run --project src/Nexo.CLI -- test --format-json --filter \"{_filter}\"",
            ct);

        // CLI may print logs + JSON; extract the JSON object containing TotalTests.
        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        var total = doc.RootElement.GetProperty("TotalTests").GetInt32();
        var failed = doc.RootElement.GetProperty("FailedTests").GetInt32();

        input.LastTestOk = exitCode == 0 && failed == 0 && total > 0;
        input.LastTestStdout = stdout;
        input.LastTestStderr = stderr;

        input.History.Add(new
        {
            step = "nexo_test",
            filter = _filter,
            exitCode,
            total,
            failed
        });

        return input;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetAsync(string workingDir, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        await p.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (p.ExitCode, stdout, stderr);
    }

    private static string ExtractJsonObject(string text)
    {
        // Take from first '{' to last '}'.
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in output");
        }
        return text.Substring(start, end - start + 1);
    }
}

