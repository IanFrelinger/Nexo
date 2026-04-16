using System.Diagnostics;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>
/// Validates the unified Runtime Studio optimize script layout and (when bash is present) CLI help output.
/// </summary>
public sealed class OptimizeAgentClusterScriptTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestScript_Exists_AndDeclaresHardwareOptimizePathAsync().ConfigureAwait(false);
            TestScript_Help_ExitsZero_WhenBashAvailable();
            return new TestResult
            {
                Name = nameof(OptimizeAgentClusterScriptTests),
                Category = "CLI",
                Passed = true,
                Message = "Optimize agent cluster script tests passed",
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(OptimizeAgentClusterScriptTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(OptimizeAgentClusterScriptTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
            };
        }
    }

    private async Task TestScript_Exists_AndDeclaresHardwareOptimizePathAsync()
    {
        var root = RepoPathResolver.FindRepoRoot();
        var script = Path.Combine(root, "apps", "runtime-studio", "scripts", "optimize_agent_cluster.sh");
        AssertTrue(File.Exists(script), $"Expected optimize script at: {script}");
        var text = await File.ReadAllTextAsync(script).ConfigureAwait(false);
        AssertTrue(
            text.Contains("Optimize agent cluster for local hardware", StringComparison.Ordinal),
            "Script should advertise the hardware optimize step.");
        AssertTrue(text.Contains("workflow optimize", StringComparison.Ordinal), "Script should invoke workflow optimize.");
        AssertTrue(
            text.Contains("bootstrap_runtime_studio.sh", StringComparison.Ordinal),
            "Script should chain bootstrap_runtime_studio.sh.");
        AssertTrue(
            text.Contains("--budget-runs", StringComparison.Ordinal),
            "Script should support --budget-runs for bounded optimize runs.");
        AssertTrue(
            text.Contains("--skip-apply-agent-set", StringComparison.Ordinal),
            "Script should allow skipping agent-set sync after optimize.");
        AssertTrue(
            text.Contains("runtime-studio apply-tune", StringComparison.Ordinal),
            "Script should sync the agent set via nexo runtime-studio apply-tune.");
    }

    private void TestScript_Help_ExitsZero_WhenBashAvailable()
    {
        var bash = ResolveBashExecutable();
        if (bash is null)
        {
            AssertTrue(true, "Skip: no bash executable found for --help smoke.");
            return;
        }

        var root = RepoPathResolver.FindRepoRoot();
        var psi = new ProcessStartInfo
        {
            FileName = bash,
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add("bash \"apps/runtime-studio/scripts/optimize_agent_cluster.sh\" --help");

        using var p = Process.Start(psi);
        AssertNotNull(p, "Failed to start bash for script --help");
        var stdout = p!.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(TimeSpan.FromSeconds(30));
        AssertEqual(0, p.ExitCode, $"optimize_agent_cluster.sh --help should exit 0. stderr={stderr}");
        AssertTrue(
            stdout.Contains("Unified workflow", StringComparison.Ordinal) ||
            stdout.Contains("optimize", StringComparison.OrdinalIgnoreCase),
            "Help output should describe the unified workflow.");
    }

    private static string? ResolveBashExecutable()
    {
        foreach (var candidate in new[]
                 {
                     @"C:\Program Files\Git\bin\bash.exe",
                     "/usr/bin/bash",
                     "/bin/bash",
                 })
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
