using System.Diagnostics;
using System.Text.Json;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for the CLI demo commands (offline mode).
/// </summary>
public sealed class DemoCliE2ETests : UnitTestBase
{
    private string? _repoRoot;
    private string? _tempProjectDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _repoRoot = TestPaths.FindRepoRoot();
        _tempProjectDir = Path.Combine(Path.GetTempPath(), "nexo-demo-e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempProjectDir);
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempProjectDir != null && Directory.Exists(_tempProjectDir))
        {
            try { Directory.Delete(_tempProjectDir, recursive: true); } catch { /* ignore */ }
        }
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AssertNotNull(_repoRoot, "Repo root should be resolved");
            AssertNotNull(_tempProjectDir, "Temp dir should be created");

            await TestDemoTestOfflineAsync(cancellationToken);
            await TestDemoDevOfflineAndResumeAsync(cancellationToken);
            await TestDemoSelfExtendAsync(cancellationToken);

            return new TestResult
            {
                Name = nameof(DemoCliE2ETests),
                Category = "E2E",
                Passed = true,
                Message = "CLI demo E2E tests passed (offline)"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(DemoCliE2ETests),
                Category = "E2E",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(DemoCliE2ETests),
                Category = "E2E",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestDemoTestOfflineAsync(CancellationToken ct)
    {
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: "run --project src/Nexo.CLI -- demo test --format-json --max-duration 1m --provider offline",
            ct);

        AssertEqual(0, code, $"demo test should exit 0. stderr: {stderr}");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        var score = doc.RootElement.GetProperty("Summary").GetProperty("OverallScore").GetDouble();
        AssertTrue(score >= 0, "OverallScore should exist");
    }

    private async Task TestDemoDevOfflineAndResumeAsync(CancellationToken ct)
    {
        // Create a tiny .NET console project to modify
        var (newCode, _, newErr) = await RunDotnetAsync(
            workingDir: _tempProjectDir!,
            args: "new console --framework net8.0",
            ct);
        AssertEqual(0, newCode, $"dotnet new should succeed. stderr: {newErr}");

        var sessionPath = Path.Combine(_tempProjectDir!, "dev-session.json");

        var (code, _, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- demo dev --project \"{_tempProjectDir}\" --format-json --yes --max-iterations 1 --provider offline --test-target \"cli://dotnet --version\" --output \"{sessionPath}\"",
            ct);
        AssertEqual(0, code, $"demo dev should exit 0. stderr: {stderr}");
        AssertTrue(File.Exists(sessionPath), "Session output file should exist");

        var json = await File.ReadAllTextAsync(sessionPath, ct);
        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("Status").GetInt32();
        AssertEqual(1, status, "SessionStatus should be Completed (1)");

        // Resume should not crash and should exit cleanly
        var (resumeCode, _, resumeErr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- demo dev --project \"{_tempProjectDir}\" --format-json --yes --max-iterations 1 --provider offline --resume \"{sessionPath}\"",
            ct);
        AssertEqual(0, resumeCode, $"demo dev --resume should exit 0. stderr: {resumeErr}");
    }

    private async Task TestDemoSelfExtendAsync(CancellationToken ct)
    {
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: "run --project src/Nexo.CLI -- demo self-extend --format-json --max-phases 5",
            ct);
        AssertEqual(0, code, $"demo self-extend should exit 0. stderr: {stderr}");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "self-extend ok should be true");

        var command = doc.RootElement.GetProperty("command").GetString();
        AssertNotNull(command, "Command should be present");

        // `command` is emitted like "demo hello-gen-xxxx"
        var (cmdCode, cmdOut, cmdErr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- {command} --format-json",
            ct);
        AssertEqual(0, cmdCode, $"generated command should exit 0. stderr: {cmdErr}");

        var cmdJson = ExtractJsonObject(cmdOut);
        using var cmdDoc = JsonDocument.Parse(cmdJson);
        AssertTrue(cmdDoc.RootElement.GetProperty("ok").GetBoolean(), "generated command ok should be true");
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
        // CLI emits logs + a single JSON object; take from first '{' to last '}'.
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in output");
        }
        return text.Substring(start, end - start + 1);
    }
}

