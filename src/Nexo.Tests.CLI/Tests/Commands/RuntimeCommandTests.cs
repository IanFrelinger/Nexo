using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Nexo.CLI.Commands;
using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

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
            AssertNotNull(resolveMethod, "Expected ResolveVisualRequired private helper.");

            var autoRequires = (bool)resolveMethod!.Invoke(null, new object[] { "auto", repoRoot, 20, 2 })!;
            AssertTrue(autoRequires, "Auto mode should require visual lane from strict benchmark streak.");

            var advisoryOnly = (bool)resolveMethod.Invoke(null, new object[] { "auto", repoRoot, 20, 3 })!;
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
        var path = Path.Combine(Path.GetTempPath(), $"nexo-runtime-command-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<(int ExitCode, string StdOut)> InvokeRuntimeAsync(string args)
    {
        var root = new RootCommand();
        root.AddCommand(new RuntimeCommand());
        var previousOut = Console.Out;
        var previousError = Console.Error;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(output);
            var exitCode = await root.InvokeAsync($"runtime {args}").ConfigureAwait(false);
            return (exitCode, output.ToString());
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
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

        throw new InvalidOperationException("No JSON payload found in runtime command output.");
    }
}
