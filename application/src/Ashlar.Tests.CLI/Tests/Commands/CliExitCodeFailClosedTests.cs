using System.CommandLine;
using Ashlar.CLI.Commands;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// #455: handlers that only set Environment.ExitCode lose the refusal —
/// System.CommandLine overwrites it back to 0 after the handler returns.
/// </summary>
public sealed class CliExitCodeFailClosedTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestComposeInvalidSupportLevelExitsNonZeroAsync().ConfigureAwait(false);
            await TestDockerPsWithoutEngineExitsNonZeroAsync().ConfigureAwait(false);
            await TestObserveEmptyWatchPathsExitsNonZeroAsync().ConfigureAwait(false);
            await TestIngestFailuresMissingTrxExitsNonZeroAsync().ConfigureAwait(false);
            await TestAdaptUnknownBrickExitsNonZeroAsync().ConfigureAwait(false);
            await TestAnalyzeBricksViolationExitsNonZeroAsync().ConfigureAwait(false);
            await TestImproveDryRunViolationExitsNonZeroAsync().ConfigureAwait(false);
            await TestImproveMissingStoreCreatesDirectoryWithoutStackTraceAsync().ConfigureAwait(false);
            await TestSelfContextInvalidLookbackExitsNonZeroWithoutStackTraceAsync().ConfigureAwait(false);
            await TestChangelogInvalidSinceExitsNonZeroWithoutStackTraceAsync().ConfigureAwait(false);
            await TestObserveInvalidDurationExitsNonZeroWithoutStackTraceAsync().ConfigureAwait(false);
            await TestTrustAuditInvalidSinceExitsNonZeroWithoutQueryingLogAsync().ConfigureAwait(false);
            await TestBackgroundAgentLogsInvalidSinceExitsNonZeroWithoutListingAsync().ConfigureAwait(false);
            await TestWorkflowReportInvalidSinceExitsNonZeroWithoutListingAsync().ConfigureAwait(false);
            await TestChangelogMissingOutputParentCreatesDirectoryWithoutStackTraceAsync().ConfigureAwait(false);
            await TestRuntimePlanInvalidQaPolicyExitsNonZeroAsync().ConfigureAwait(false);
            await TestRuntimeGateInvalidPolicyExitsNonZeroAsync().ConfigureAwait(false);
            await TestRuntimePlanInvalidManifestQaPolicyExitsNonZeroAsync().ConfigureAwait(false);
            await TestBootstrapInvalidProfileExitsNonZeroAsync().ConfigureAwait(false);
            await TestDoctorInvalidProfileExitsNonZeroAsync().ConfigureAwait(false);
            await TestSelfExtendInvalidFocusExitsNonZeroAsync().ConfigureAwait(false);
            await TestSelfExtendInvalidVisualQaFallbackPolicyExitsNonZeroAsync().ConfigureAwait(false);
            await TestChatInvalidPreferModelExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowStressInvalidPreferExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowOptimizeInvalidSearchStrategyExitsNonZeroAsync().ConfigureAwait(false);
            await TestImproveInvalidAutonomyExitsNonZeroAsync().ConfigureAwait(false);
            await TestMultiEnvInvalidSuiteExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowOptimizeInvalidSpecPreferExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowStressInvalidSpecPreferExitsNonZeroAsync().ConfigureAwait(false);
            await TestRuntimePlanInvalidMaxIterationsExitsNonZeroAsync().ConfigureAwait(false);
            await TestSelfExtendPreflightInvalidMaxIterationsExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowOptimizeInvalidIterationsExitsNonZeroAsync().ConfigureAwait(false);
            await TestRuntimeHistoryInvalidLimitExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowHistoryInvalidLimitExitsNonZeroAsync().ConfigureAwait(false);
            await TestRuntimeGateInvalidMinTotalExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowOptimizeInvalidMaxCandidatesExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowOptimizeInvalidBudgetRunsExitsNonZeroAsync().ConfigureAwait(false);
            await TestWorkflowOptimizeInvalidEarlyStopMinRunsExitsNonZeroAsync().ConfigureAwait(false);
            await TestRuntimeReleaseGateInvalidLaneRepetitionsExitsNonZeroAsync().ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(CliExitCodeFailClosedTests),
                Category = "CLI",
                Passed = true,
                Message = "CLI exit-code fail-closed tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(CliExitCodeFailClosedTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(CliExitCodeFailClosedTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestComposeInvalidSupportLevelExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ComposeCommand());
            var exitCode = await root.InvokeAsync("compose --problem test --support-level not-a-level").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "Invalid --support-level must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestDockerPsWithoutEngineExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new DockerCommand());
            var exitCode = await root.InvokeAsync("docker ps").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "docker ps without an engine must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestObserveEmptyWatchPathsExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-observe-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ObserveCommand());
            var exitCode = await root.InvokeAsync($"observe --path {tempRoot} --duration 1s").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "observe with no watch paths must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestIngestFailuresMissingTrxExitsNonZeroAsync()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"ashlar-no-trx-{Guid.NewGuid():N}");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new IngestFailuresCommand());
            var exitCode = await root.InvokeAsync($"ingest-failures --trx-path {missing}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "ingest-failures with no TRX files must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestAdaptUnknownBrickExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-adapt-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new AdaptCommand());
            var exitCode = await root.InvokeAsync($"adapt --brick not-a-brick --store-path {tempRoot}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "adapt with an unknown brick must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestAnalyzeBricksViolationExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-bricks-empty-catch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var source = Path.Combine(tempRoot, "Bad.cs");
        await File.WriteAllTextAsync(source, "class Bad { void M() { try { } catch { } } }").ConfigureAwait(false);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new AnalyzeBricksCommand());
            var exitCode = await root.InvokeAsync($"bricks --path {source}").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "analyze bricks with a violation must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestImproveDryRunViolationExitsNonZeroAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-improve-dryrun-{Guid.NewGuid():N}");
        var stateDir = Path.Combine(tempRoot, "state");
        Directory.CreateDirectory(stateDir);
        var source = Path.Combine(tempRoot, "Bad.cs");
        await File.WriteAllTextAsync(source, "class Bad { void M() { try { } catch { } } }").ConfigureAwait(false);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ImproveCommand());
            var exitCode = await root.InvokeAsync($"improve --dry-run --path {source} --store-path {stateDir} --yes").ConfigureAwait(false);
            AssertTrue(exitCode != 0, "improve --dry-run with a violation must exit non-zero, not 0.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestImproveMissingStoreCreatesDirectoryWithoutStackTraceAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-improve-store-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var stateDir = Path.Combine(tempRoot, "state");
        var source = Path.Combine(tempRoot, "Empty.cs");
        await File.WriteAllTextAsync(source, "class C {}").ConfigureAwait(false);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ImproveCommand());
            var exitCode = await root.InvokeAsync($"improve --dry-run --path {source} --store-path {stateDir} --yes").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode == 0, "improve --dry-run on clean source must exit 0 after creating --store-path.");
            AssertTrue(Directory.Exists(stateDir), "--store-path must be created instead of crashing.");
            AssertTrue(!output.Contains("DirectoryNotFoundException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "A missing --store-path must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestSelfContextInvalidLookbackExitsNonZeroWithoutStackTraceAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new SelfContextCommand());
            var exitCode = await root.InvokeAsync("self-context --lookback xh").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "self-context --lookback xh must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --lookback", StringComparison.Ordinal),
                "An invalid --lookback must be refused legibly.");
            AssertTrue(!output.Contains("FormatException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "An invalid --lookback must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestChangelogInvalidSinceExitsNonZeroWithoutStackTraceAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ChangelogCommand());
            var exitCode = await root.InvokeAsync("changelog --since xh").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "changelog --since xh must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --since", StringComparison.Ordinal),
                "An invalid --since must be refused legibly.");
            AssertTrue(!output.Contains("FormatException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "An invalid --since must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestObserveInvalidDurationExitsNonZeroWithoutStackTraceAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-observe-duration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ObserveCommand());
            var exitCode = await root.InvokeAsync($"observe --path {tempRoot} --duration xm").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "observe --duration xm must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --duration", StringComparison.Ordinal),
                "An invalid --duration must be refused legibly.");
            AssertTrue(!output.Contains("No watch paths", StringComparison.Ordinal),
                "An invalid --duration must be refused before the watch-path check.");
            AssertTrue(!output.Contains("FormatException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "An invalid --duration must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task TestTrustAuditInvalidSinceExitsNonZeroWithoutQueryingLogAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var auditLog = new Ashlar.BackgroundAgents.Trust.DataDecisionAuditLog();
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TrustCommand>.Instance;
            var command = new TrustCommand(auditLog, null, null, logger);
            var exitCode = await command.AuditAsync(10, "xyz", null, null, false, false, false).ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "trust audit --since xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --since", StringComparison.Ordinal),
                "An invalid --since must be refused legibly.");
            AssertTrue(!output.Contains("Data Decision Audit", StringComparison.Ordinal),
                "An invalid --since must be refused before listing audit entries.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestBackgroundAgentLogsInvalidSinceExitsNonZeroWithoutListingAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("background-agent logs --id any-agent --since xyz").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "background-agent logs --since xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --since", StringComparison.Ordinal),
                "An invalid --since must be refused legibly.");
            AssertTrue(!output.Contains("No logs for agent", StringComparison.Ordinal),
                "An invalid --since must be refused before listing logs.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowReportInvalidSinceExitsNonZeroWithoutListingAsync()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), $"ashlar-wf-since-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new WorkflowCommand((_, _, _, _, _, _) =>
                Task.FromResult(new WorkflowCommand.ScenarioExecutionResult(
                    Ok: true,
                    Summary: "stub",
                    ConflictCount: 0,
                    EscalationCount: 0))));
            var exitCode = await root.InvokeAsync($"workflow report --repo-root {repoRoot} --since xyz").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow report --since xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --since", StringComparison.Ordinal),
                "An invalid --since must be refused legibly.");
            AssertTrue(!output.Contains("No workflow stress history", StringComparison.Ordinal),
                "An invalid --since must be refused before the history lookup.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestChangelogMissingOutputParentCreatesDirectoryWithoutStackTraceAsync()
    {
        var dest = Path.Combine(Path.GetTempPath(), $"ashlar-changelog-{Guid.NewGuid():N}", "out.md");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ChangelogCommand());
            var exitCode = await root.InvokeAsync($"changelog --output {dest}").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode == 0, "changelog --output must create a missing parent directory and exit 0.");
            AssertTrue(File.Exists(dest), "changelog --output must write the file after creating the parent directory.");
            AssertTrue(!output.Contains("DirectoryNotFoundException", StringComparison.Ordinal)
                && !output.Contains("   at ", StringComparison.Ordinal),
                "A missing changelog --output parent directory must not print a stack trace.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            var parent = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                Directory.Delete(parent, recursive: true);
        }
    }

    private async Task TestRuntimePlanInvalidQaPolicyExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new RuntimeCommand());
            var exitCode = await root.InvokeAsync("runtime plan --goal test --qa-policy xyz --use-history false").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime plan --qa-policy xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --qa-policy", StringComparison.Ordinal),
                "An invalid --qa-policy must be refused legibly.");
            AssertTrue(!output.Contains("Plan computed successfully", StringComparison.Ordinal),
                "An invalid --qa-policy must be refused before computing a plan.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestRuntimePlanInvalidManifestQaPolicyExitsNonZeroAsync()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"ashlar-runtime-manifest-{Guid.NewGuid():N}.json");
        File.WriteAllText(manifestPath, """{"qaPolicyProfile":"xyz"}""");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new RuntimeCommand());
            var exitCode = await root.InvokeAsync(
                $"runtime plan --goal test --use-history false --runtime-manifest {manifestPath}")
                .ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime plan with qaPolicyProfile xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid qaPolicyProfile", StringComparison.Ordinal),
                "An invalid qaPolicyProfile must be refused legibly.");
            AssertTrue(!output.Contains("Plan computed successfully", StringComparison.Ordinal),
                "An invalid qaPolicyProfile must be refused before computing a plan as demo.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (File.Exists(manifestPath))
                File.Delete(manifestPath);
        }
    }

    private async Task TestRuntimeGateInvalidPolicyExitsNonZeroAsync()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), $"ashlar-gate-policy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoRoot);
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new RuntimeCommand());
            var exitCode = await root.InvokeAsync(
                $"runtime gate --repo-root {repoRoot} --policy xyz --min-pass-rate 0 --min-total 1 --min-consecutive-passes 0").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime gate --policy xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --policy", StringComparison.Ordinal),
                "An invalid --policy must be refused legibly.");
            AssertTrue(!output.Contains("Gate passed", StringComparison.Ordinal),
                "An invalid --policy must be refused before evaluating history as auto.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestBootstrapInvalidProfileExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new BootstrapCommand());
            var exitCode = await root.InvokeAsync("bootstrap check --profile xyz --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "bootstrap check --profile xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --profile", StringComparison.Ordinal),
                "An invalid --profile must be refused legibly.");
            AssertTrue(!output.Contains("\"profile\": \"demo\"", StringComparison.Ordinal),
                "An invalid --profile must be refused before assessing as demo.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestDoctorInvalidProfileExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new DoctorCommand());
            var exitCode = await root.InvokeAsync("doctor --profile xyz --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "doctor --profile xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --profile", StringComparison.Ordinal),
                "An invalid --profile must be refused legibly.");
            AssertTrue(!output.Contains("\"overallOk\"", StringComparison.Ordinal),
                "An invalid --profile must be refused before running doctor probes.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestSelfExtendInvalidFocusExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("self-extend preflight --focus xyz --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "self-extend preflight --focus xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --focus", StringComparison.Ordinal),
                "An invalid --focus must be refused legibly.");
            AssertTrue(!output.Contains("Preflight passed", StringComparison.Ordinal),
                "An invalid --focus must be refused before running preflight as balanced.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestSelfExtendInvalidVisualQaFallbackPolicyExitsNonZeroAsync()
    {
        var specPath = Path.Combine(Path.GetTempPath(), $"ashlar-self-extend-visual-qa-{Guid.NewGuid():N}.json");
        File.WriteAllText(specPath, """{"workflow":{"visualQaFallbackPolicy":"xyz","runVisualQa":true}}""");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync($"self-extend preflight --runtime-spec {specPath} --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "self-extend preflight with visualQaFallbackPolicy xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid visualQaFallbackPolicy", StringComparison.Ordinal),
                "An invalid visualQaFallbackPolicy must be refused legibly.");
            AssertTrue(!output.Contains("Preflight passed", StringComparison.Ordinal),
                "An invalid visualQaFallbackPolicy must be refused before running preflight as strict.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (File.Exists(specPath))
                File.Delete(specPath);
        }
    }

    private async Task TestChatInvalidPreferModelExitsNonZeroAsync()
    {
        var orchestrateCalled = false;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ChatCommand(() =>
            {
                orchestrateCalled = true;
                throw new InvalidOperationException("orchestrate must not run for invalid --prefer-model");
            }));
            var exitCode = await root.InvokeAsync("chat --prefer-model xyz --prompt hello --skip-bootstrap-check").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "chat --prefer-model xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --prefer-model", StringComparison.Ordinal),
                "An invalid --prefer-model must be refused legibly.");
            AssertTrue(!orchestrateCalled,
                "An invalid --prefer-model must be refused before starting orchestration.");
            AssertTrue(!output.Contains("Chat demo-local mode enabled", StringComparison.Ordinal),
                "An invalid --prefer-model must be refused before demo-local orchestration.");
            AssertTrue(!output.Contains("Orchestration completed successfully", StringComparison.Ordinal),
                "An invalid --prefer-model must not complete orchestration.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowStressInvalidPreferExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow stress --prefer xyz --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow stress --prefer xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --prefer", StringComparison.Ordinal),
                "An invalid --prefer must be refused legibly.");
            AssertTrue(!output.Contains("Starting orchestration", StringComparison.Ordinal),
                "An invalid --prefer must be refused before starting workflow stress.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowOptimizeInvalidSearchStrategyExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow optimize --search-strategy xyz --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow optimize --search-strategy xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --search-strategy", StringComparison.Ordinal),
                "An invalid --search-strategy must be refused legibly.");
            AssertTrue(!output.Contains("\"searchStrategy\": \"successive-halving\"", StringComparison.Ordinal),
                "An invalid --search-strategy must be refused before remapping to successive-halving.");
            AssertTrue(!output.Contains("evaluated candidate", StringComparison.Ordinal),
                "An invalid --search-strategy must be refused before evaluating optimize candidates.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestImproveInvalidAutonomyExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new ImproveCommand());
            var exitCode = await root.InvokeAsync("improve --autonomy xyz --dry-run --path /tmp/improve-autonomy-xyz/Empty.cs --yes --skip-regression").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "improve --autonomy xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --autonomy", StringComparison.Ordinal),
                "An invalid --autonomy must be refused legibly.");
            AssertTrue(!output.Contains("Autonomy: xyz", StringComparison.Ordinal),
                "An invalid --autonomy must be refused before running improve as supervised.");
            AssertTrue(!output.Contains("No violations found", StringComparison.Ordinal),
                "An invalid --autonomy must be refused before analyzing source.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestMultiEnvInvalidSuiteExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = new RootCommand();
            root.AddCommand(TestMultiEnvCommand.CreateCommand());
            var exitCode = await root.InvokeAsync("multi-env --suite xyz").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "test multi-env --suite xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --suite", StringComparison.Ordinal),
                "An invalid --suite must be refused legibly.");
            AssertTrue(!output.Contains("Testing ubuntu-8.0", StringComparison.Ordinal),
                "An invalid --suite must be refused before running the framework suite.");
            AssertTrue(!output.Contains("Building ubuntu-8.0", StringComparison.Ordinal),
                "An invalid --suite must be refused before starting Docker.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private static string WriteWorkflowLabSpecWithPrefer(string prefer)
    {
        var specPath = Path.Combine(Path.GetTempPath(), $"ashlar-workflow-prefer-{Guid.NewGuid():N}.json");
        File.WriteAllText(specPath,
            $$"""
            {
              "execution": { "iterations": 1, "persistHistory": false },
              "requests": [{ "id": "hello", "prompt": "hello" }],
              "compositions": [{
                "id": "solo",
                "roles": [{ "agentId": "builder-1", "role": "builder", "goal": "Handle the request." }]
              }],
              "modelProfiles": [{
                "id": "mock",
                "default": { "prefer": "{{prefer}}", "provider": "mock-json" }
              }]
            }
            """);
        return specPath;
    }

    private async Task TestWorkflowOptimizeInvalidSpecPreferExitsNonZeroAsync()
    {
        var specPath = WriteWorkflowLabSpecWithPrefer("xyz");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync($"workflow optimize --spec {specPath} --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow optimize with prefer xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid prefer in runtime spec", StringComparison.Ordinal),
                "An invalid runtime-spec prefer must be refused legibly.");
            AssertTrue(!output.Contains("Starting orchestration", StringComparison.Ordinal),
                "An invalid runtime-spec prefer must be refused before starting optimize orchestration.");
            AssertTrue(!output.Contains("evaluated candidate", StringComparison.Ordinal),
                "An invalid runtime-spec prefer must be refused before evaluating optimize candidates.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (File.Exists(specPath))
                File.Delete(specPath);
        }
    }

    private async Task TestWorkflowStressInvalidSpecPreferExitsNonZeroAsync()
    {
        var specPath = WriteWorkflowLabSpecWithPrefer("xyz");
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync($"workflow stress --spec {specPath} --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow stress with prefer xyz must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid prefer in runtime spec", StringComparison.Ordinal),
                "An invalid runtime-spec prefer must be refused legibly.");
            AssertTrue(!output.Contains("Starting orchestration", StringComparison.Ordinal),
                "An invalid runtime-spec prefer must be refused before starting workflow stress.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
            if (File.Exists(specPath))
                File.Delete(specPath);
        }
    }

    private async Task TestRuntimePlanInvalidMaxIterationsExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("runtime plan --goal hello --max-iterations 0 --use-history false --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime plan --max-iterations 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --max-iterations", StringComparison.Ordinal),
                "A non-positive --max-iterations must be refused legibly.");
            AssertTrue(!output.Contains("Plan computed successfully", StringComparison.Ordinal),
                "A non-positive --max-iterations must be refused before computing a plan as the policy default.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestSelfExtendPreflightInvalidMaxIterationsExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("self-extend preflight --max-iterations 0 --json --allow-mock").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "self-extend preflight --max-iterations 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --max-iterations", StringComparison.Ordinal),
                "A non-positive --max-iterations must be refused legibly.");
            AssertTrue(!output.Contains("Preflight passed", StringComparison.Ordinal),
                "A non-positive --max-iterations must be refused before remapping to 1 and running preflight.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowOptimizeInvalidIterationsExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow optimize --iterations 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow optimize --iterations 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --iterations", StringComparison.Ordinal),
                "A non-positive --iterations must be refused legibly.");
            AssertTrue(!output.Contains("evaluated candidate", StringComparison.Ordinal),
                "A non-positive --iterations must be refused before remapping to 1 and evaluating candidates.");
            AssertTrue(!output.Contains("Starting orchestration", StringComparison.Ordinal),
                "A non-positive --iterations must be refused before starting optimize orchestration.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestRuntimeHistoryInvalidLimitExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("runtime history --limit 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime history --limit 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --limit", StringComparison.Ordinal),
                "A non-positive --limit must be refused legibly.");
            AssertTrue(!output.Contains("Loaded 1 history entries", StringComparison.Ordinal),
                "A --limit of 0 must not be remapped to 1 before reading runtime history.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowHistoryInvalidLimitExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow history --limit 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow history --limit 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --limit", StringComparison.Ordinal),
                "A non-positive --limit must be refused legibly.");
            AssertTrue(!output.Contains("Loaded 1 workflow stress history entries", StringComparison.Ordinal),
                "A --limit of 0 must not be remapped to 1 before reading workflow history.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestRuntimeGateInvalidMinTotalExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("runtime gate --min-total 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime gate --min-total 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --min-total", StringComparison.Ordinal),
                "A non-positive --min-total must be refused legibly.");
            AssertTrue(!output.Contains("Gate passed", StringComparison.Ordinal),
                "A --min-total of 0 must not be remapped to 1 before the gate can pass.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowOptimizeInvalidMaxCandidatesExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow optimize --max-candidates 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow optimize --max-candidates 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --max-candidates", StringComparison.Ordinal),
                "A non-positive --max-candidates must be refused legibly.");
            AssertTrue(!output.Contains("evaluated candidate", StringComparison.Ordinal),
                "A --max-candidates of 0 must not be remapped to 1 before evaluating candidates.");
            AssertTrue(!output.Contains("Starting orchestration", StringComparison.Ordinal),
                "A --max-candidates of 0 must not start optimize orchestration.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowOptimizeInvalidBudgetRunsExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow optimize --budget-runs 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow optimize --budget-runs 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --budget-runs", StringComparison.Ordinal),
                "A non-positive --budget-runs must be refused legibly.");
            AssertTrue(!output.Contains("\"budgetRuns\": 1", StringComparison.Ordinal),
                "A --budget-runs of 0 must not be remapped to 1 before evaluating candidates.");
            AssertTrue(!output.Contains("evaluated candidate", StringComparison.Ordinal),
                "A --budget-runs of 0 must not be remapped to 1 before evaluating candidates.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestWorkflowOptimizeInvalidEarlyStopMinRunsExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("workflow optimize --early-stop-min-runs 0 --json").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "workflow optimize --early-stop-min-runs 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --early-stop-min-runs", StringComparison.Ordinal),
                "A non-positive --early-stop-min-runs must be refused legibly.");
            AssertTrue(!output.Contains("\"earlyStopMinRuns\": 1", StringComparison.Ordinal),
                "A --early-stop-min-runs of 0 must not be remapped to 1 before evaluating candidates.");
            AssertTrue(!output.Contains("evaluated candidate", StringComparison.Ordinal),
                "A --early-stop-min-runs of 0 must not start candidate evaluation.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestRuntimeReleaseGateInvalidLaneRepetitionsExitsNonZeroAsync()
    {
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        try
        {
            var root = Ashlar.CLI.Program.BuildRootCommand();
            var exitCode = await root.InvokeAsync("runtime release-gate --mode core --lane-repetitions 0 --allow-mock").ConfigureAwait(false);
            var output = writer.ToString();
            AssertTrue(exitCode != 0, "runtime release-gate --lane-repetitions 0 must exit non-zero, not 0.");
            AssertTrue(output.Contains("Invalid --lane-repetitions", StringComparison.Ordinal),
                "A non-positive --lane-repetitions must be refused legibly.");
            AssertTrue(!output.Contains("release-core matrix", StringComparison.Ordinal),
                "A --lane-repetitions of 0 must not be remapped to 1 before starting the release-core matrix.");
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }
}
