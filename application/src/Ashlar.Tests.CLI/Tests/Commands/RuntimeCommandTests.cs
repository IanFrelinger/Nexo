using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Runtime;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for runtime command.</summary>
public sealed class RuntimeCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestHistoryFiltersByBenchmarkSetAsync().ConfigureAwait(false);
            await TestGateRequiresConsecutivePassStreakAsync().ConfigureAwait(false);
            await TestGateJsonIncludesSloEvidenceAsync().ConfigureAwait(false);
            await TestReleaseGateRejectsInvalidModeAsync().ConfigureAwait(false);
            await TestPlanRejectsInvalidQaPolicyAsync().ConfigureAwait(false);
            await TestPlanRejectsInvalidBootstrapProfileAsync().ConfigureAwait(false);
            await TestGateRejectsInvalidPolicyAsync().ConfigureAwait(false);
            await TestPlanRejectsInvalidManifestQaPolicyAsync().ConfigureAwait(false);
            await TestPlanRejectsInvalidMaxIterationsAsync().ConfigureAwait(false);
            /// <summary>Test visual required auto uses strict benchmark set.</summary>
            TestVisualRequiredAutoUsesStrictBenchmarkSet();

            return new TestResult
            {
                Name = nameof(RuntimeCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "Runtime command benchmark and streak tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(RuntimeCommandTests),
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
                Name = nameof(RuntimeCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestHistoryFiltersByBenchmarkSetAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now,
                ElapsedMs = 101,
                GoalFingerprint = "goal-a",
                GoalPreview = "goal-a",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now.AddSeconds(-1),
                ElapsedMs = 111,
                GoalFingerprint = "goal-b",
                GoalPreview = "goal-b",
                BenchmarkSet = "release-visual",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });

            var (exitCode, output) = await InvokeRuntimeAsync(
                $"history --repo-root \"{repoRoot}\" --benchmark-set release-core --limit 20 --json").ConfigureAwait(false);

            /// <summary>Assert equal.</summary>
            AssertEqual(0, exitCode);
            using var payload = ParseLastJsonObject(output);
            var root = payload.RootElement;
            AssertTrue(root.GetProperty("ok").GetBoolean(), "History command should succeed.");
            var summaryStats = root.GetProperty("summaryStats");
            AssertEqual(1, summaryStats.GetProperty("Total").GetInt32(), "Expected benchmark filter to return only one item.");
            var items = root.GetProperty("items");
            AssertEqual(1, items.GetArrayLength());
            AssertEqual("release-core", items[0].GetProperty("BenchmarkSet").GetString() ?? string.Empty);
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestGateRequiresConsecutivePassStreakAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now.AddMinutes(-2),
                ElapsedMs = 90,
                GoalFingerprint = "goal-x",
                GoalPreview = "goal-x",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = false,
                FailureStage = "preflight"
            });
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now.AddMinutes(-1),
                ElapsedMs = 91,
                GoalFingerprint = "goal-y",
                GoalPreview = "goal-y",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now,
                ElapsedMs = 92,
                GoalFingerprint = "goal-z",
                GoalPreview = "goal-z",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });

            var (strictExit, strictOutput) = await InvokeRuntimeAsync(
                $"gate --repo-root \"{repoRoot}\" --benchmark-set release-core --policy release --history-window 20 --min-pass-rate 0 --min-total 1 --min-consecutive-passes 3 --json")
                .ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
            /// <param name="threshold."">Threshold.".</param>
            AssertEqual(1, strictExit, "Gate should fail when streak is below required threshold.");
            using (var strictPayload = ParseLastJsonObject(strictOutput))
            {
                var strictRoot = strictPayload.RootElement;
                AssertFalse(strictRoot.GetProperty("ok").GetBoolean());
                AssertEqual(2, strictRoot.GetProperty("streak").GetInt32());
                AssertEqual(3, strictRoot.GetProperty("minConsecutivePasses").GetInt32());
            }

            var (lenientExit, lenientOutput) = await InvokeRuntimeAsync(
                $"gate --repo-root \"{repoRoot}\" --benchmark-set release-core --policy release --history-window 20 --min-pass-rate 0 --min-total 1 --min-consecutive-passes 2 --json")
                .ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
            /// <param name="met."">Met.".</param>
            AssertEqual(0, lenientExit, "Gate should pass when streak threshold is met.");
            using var lenientPayload = ParseLastJsonObject(lenientOutput);
            var lenientRoot = lenientPayload.RootElement;
            AssertTrue(lenientRoot.GetProperty("ok").GetBoolean());
            AssertEqual(2, lenientRoot.GetProperty("streak").GetInt32());
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestReleaseGateRejectsInvalidModeAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            var (exitCode, output) = await InvokeRuntimeAsync(
                $"release-gate --mode bananas --repo-root \"{repoRoot}\"").ConfigureAwait(false);
            /// <summary>Assert equal.</summary>
            AssertEqual(1, exitCode);
            AssertTrue(output.Contains("unsupported mode", StringComparison.OrdinalIgnoreCase),
                "Expected release-gate to reject unsupported mode values.");
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestPlanRejectsInvalidQaPolicyAsync()
    {
        var (exitCode, output) = await InvokeRuntimeAsync("plan --goal test --qa-policy xyz --use-history false --json").ConfigureAwait(false);
        AssertEqual(1, exitCode);
        AssertTrue(output.Contains("Invalid --qa-policy", StringComparison.Ordinal),
            "An invalid --qa-policy must be refused legibly.");
        AssertTrue(!output.Contains("Plan computed successfully", StringComparison.Ordinal),
            "An invalid --qa-policy must be refused before computing a plan.");
    }

    private async Task TestPlanRejectsInvalidBootstrapProfileAsync()
    {
        var (exitCode, output) = await InvokeRuntimeAsync("plan --goal test --bootstrap-profile xyz --use-history false --json").ConfigureAwait(false);
        AssertEqual(1, exitCode);
        AssertTrue(output.Contains("Invalid --bootstrap-profile", StringComparison.Ordinal),
            "An invalid --bootstrap-profile must be refused legibly.");
        AssertTrue(!output.Contains("Plan computed successfully", StringComparison.Ordinal),
            "An invalid --bootstrap-profile must be refused before computing a plan.");
    }

    private async Task TestPlanRejectsInvalidManifestQaPolicyAsync()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"ashlar-runtime-manifest-{Guid.NewGuid():N}.json");
        File.WriteAllText(manifestPath, """{"qaPolicyProfile":"xyz"}""");
        try
        {
            var (exitCode, output) = await InvokeRuntimeAsync(
                $"plan --goal test --use-history false --runtime-manifest \"{manifestPath}\" --json")
                .ConfigureAwait(false);
            AssertEqual(1, exitCode);
            AssertTrue(output.Contains("Invalid qaPolicyProfile", StringComparison.Ordinal),
                "An invalid runtime-manifest qaPolicyProfile must be refused legibly.");
            AssertTrue(!output.Contains("Plan computed successfully", StringComparison.Ordinal),
                "An invalid qaPolicyProfile must be refused before computing a plan as demo.");
        }
        finally
        {
            if (File.Exists(manifestPath))
                File.Delete(manifestPath);
        }
    }

    private async Task TestPlanRejectsInvalidMaxIterationsAsync()
    {
        var (zeroExit, zeroOutput) = await InvokeRuntimeAsync("plan --goal test --max-iterations 0 --use-history false --json").ConfigureAwait(false);
        AssertEqual(1, zeroExit);
        AssertTrue(zeroOutput.Contains("Invalid --max-iterations", StringComparison.Ordinal),
            "A non-positive --max-iterations must be refused legibly.");
        AssertTrue(!zeroOutput.Contains("Plan computed successfully", StringComparison.Ordinal),
            "A non-positive --max-iterations must be refused before computing a plan.");
        AssertTrue(!zeroOutput.Contains("\"maxIterations\": 2", StringComparison.Ordinal),
            "A --max-iterations of 0 must not be dropped so the demo policy default of 2 is planned.");

        var (negExit, negOutput) = await InvokeRuntimeAsync("plan --goal test --max-iterations -3 --use-history false --json").ConfigureAwait(false);
        AssertEqual(1, negExit);
        AssertTrue(negOutput.Contains("Invalid --max-iterations", StringComparison.Ordinal),
            "A negative --max-iterations must be refused legibly.");
        AssertTrue(!negOutput.Contains("Plan computed successfully", StringComparison.Ordinal),
            "A negative --max-iterations must be refused before computing a plan.");
    }

    private async Task TestGateRejectsInvalidPolicyAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = DateTimeOffset.UtcNow,
                ElapsedMs = 80,
                GoalFingerprint = "goal-auto",
                GoalPreview = "goal-auto",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "auto",
                ResolvedQaPolicy = "auto",
                Success = true,
                FailureStage = "none"
            });

            var (exitCode, output) = await InvokeRuntimeAsync(
                $"gate --repo-root \"{repoRoot}\" --policy xyz --benchmark-set release-core --history-window 20 --min-pass-rate 0 --min-total 1 --min-consecutive-passes 0 --json")
                .ConfigureAwait(false);
            AssertEqual(1, exitCode);
            AssertTrue(output.Contains("Invalid --policy", StringComparison.Ordinal),
                "An invalid --policy must be refused legibly.");
            AssertTrue(!output.Contains("Gate passed", StringComparison.Ordinal),
                "An invalid --policy must be refused before evaluating history as auto.");
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private async Task TestGateJsonIncludesSloEvidenceAsync()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now,
                ElapsedMs = 120,
                GoalFingerprint = "goal-slo-a",
                GoalPreview = "goal-slo-a",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now.AddSeconds(-1),
                ElapsedMs = 250,
                GoalFingerprint = "goal-slo-b",
                GoalPreview = "goal-slo-b",
                BenchmarkSet = "release-core",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = false,
                FailureStage = "self-extend"
            });

            var (exitCode, output) = await InvokeRuntimeAsync(
                $"gate --repo-root \"{repoRoot}\" --benchmark-set release-core --policy release --history-window 20 --min-pass-rate 0 --min-total 1 --min-consecutive-passes 0 --json")
                .ConfigureAwait(false);

            /// <summary>Assert equal.</summary>
            /// <param name="thresholds."">Thresholds.".</param>
            AssertEqual(0, exitCode, "Gate should pass with relaxed thresholds.");
            using var payload = ParseLastJsonObject(output);
            var root = payload.RootElement;
            AssertTrue(root.TryGetProperty("sloEvidence", out var evidence), "Gate JSON should include sloEvidence.");
            AssertEqual(2, evidence.GetProperty("TotalSamples").GetInt32());
            AssertTrue(evidence.GetProperty("NcrFailureRate").GetDouble() >= 0d, "Failure rate should be present.");
            AssertTrue(evidence.TryGetProperty("Checks", out var checks), "Checks should be present.");
            AssertTrue(checks.GetArrayLength() >= 1, "At least one SLO check should be emitted.");
            AssertTrue(evidence.TryGetProperty("Lanes", out var lanes), "Lane evidence should be present.");
            AssertTrue(lanes.GetArrayLength() >= 1, "At least one lane should be emitted.");
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private void TestVisualRequiredAutoUsesStrictBenchmarkSet()
    {
        var repoRoot = CreateTempRepoRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now,
                BenchmarkSet = "release-visual-strict",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now.AddSeconds(-1),
                BenchmarkSet = "release-visual-strict",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = true,
                FailureStage = "none"
            });
            AdaptiveRuntimeExecutionHistoryStore.Append(repoRoot, new AdaptiveRuntimeExecutionReport
            {
                StartedAtUtc = now.AddSeconds(-2),
                BenchmarkSet = "release-visual-degraded",
                RequestedQaPolicy = "release",
                ResolvedQaPolicy = "release",
                Success = false,
                FailureStage = "preflight"
            });

            var resolveMethod = typeof(RuntimeCommand).GetMethod(
                "ResolveVisualRequired",
                BindingFlags.NonPublic | BindingFlags.Static);
            /// <summary>Assert not null.</summary>
            /// <param name="helper."">Helper.".</param>
            AssertNotNull(resolveMethod, "Expected ResolveVisualRequired private helper.");

            var autoRequires = (bool)resolveMethod!.Invoke(null, new object[] { "auto", repoRoot, 20, 2 })!;
            /// <summary>Assert true.</summary>
            /// <param name="streak."">Streak.".</param>
            AssertTrue(autoRequires, "Auto mode should require visual lane from strict benchmark streak.");

            var advisoryOnly = (bool)resolveMethod.Invoke(null, new object[] { "auto", repoRoot, 20, 3 })!;
            /// <summary>Assert false.</summary>
            /// <param name="insufficient."">Insufficient.".</param>
            AssertFalse(advisoryOnly, "Auto mode should not require visual lane when strict streak is insufficient.");
        }
        finally
        {
            if (Directory.Exists(repoRoot))
                Directory.Delete(repoRoot, recursive: true);
        }
    }

    private static string CreateTempRepoRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-runtime-command-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<(int ExitCode, string StdOut)> InvokeRuntimeAsync(string args)
    {
        var root = new RootCommand();
        root.AddCommand(new RuntimeCommand());
        var output = new StringWriter();  // not disposed on purpose: a disposed writer left in Console.Out poisons later tests
        try
        {
            Console.SetOut(output);
            Console.SetError(output);
            var exitCode = await root.InvokeAsync($"runtime {args}").ConfigureAwait(false);
            return (exitCode, output.ToString());
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private static JsonDocument ParseLastJsonObject(string text)
    {
        var source = text ?? string.Empty;
        var idx = source.LastIndexOf('{');
        while (idx >= 0)
        {
            var candidate = source[idx..].Trim();
            try
            {
                return JsonDocument.Parse(candidate);
            }
            catch
            {
                idx = idx > 0 ? source.LastIndexOf('{', idx - 1) : -1;
            }
        }

        /// <summary>Invalid operation exception.</summary>
        /// <param name="output."">Output.".</param>
        throw new InvalidOperationException("No JSON payload found in runtime command output.");
    }
}
