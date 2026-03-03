using System.Diagnostics;
using System.Text.Json;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands.DemoGenerated;

public sealed class HelloGen87e4b530CommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var repoRoot = FindRepoRoot();
            var (code, stdout, stderr) = await RunDotnetAsync(
                repoRoot,
                "run --project src/Nexo.CLI -- demo hello-gen-87e4b530 --format-json",
                cancellationToken);

            AssertEqual(0, code, $"Command should exit 0. stderr: {stderr}");

            var json = ExtractJsonObject(stdout);
            using var doc = JsonDocument.Parse(json);
            AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "ok should be true");
            AssertEqual("hello-gen-87e4b530", doc.RootElement.GetProperty("command").GetString());
            AssertEqual("hello from generated command", doc.RootElement.GetProperty("message").GetString());

            return new TestResult
            {
                Name = nameof(HelloGen87e4b530CommandTests),
                Category = "DemoSelfExtend",
                Passed = true,
                Message = "Generated command works"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(HelloGen87e4b530CommandTests),
                Category = "DemoSelfExtend",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(HelloGen87e4b530CommandTests),
                Category = "DemoSelfExtend",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate Nexo.sln");
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
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in output");
        }
        return text.Substring(start, end - start + 1);
    }
}