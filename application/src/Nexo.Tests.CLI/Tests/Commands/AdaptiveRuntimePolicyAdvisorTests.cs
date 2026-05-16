using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public sealed class AdaptiveRuntimePolicyAdvisorTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestNoHistoryRecommendation();
            TestVisualPreflightFailureRecommendation();
            TestSelfExtendFailureRecommendation();
            TestHistoryStoreRoundTrip();

            return Task.FromResult(new TestResult
            {
                Name = nameof(AdaptiveRuntimePolicyAdvisorTests),
                Category = "CLI",
                Passed = true,
                Message = "Adaptive runtime policy advisor tests passed"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AdaptiveRuntimePolicyAdvisorTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AdaptiveRuntimePolicyAdvisorTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestNoHistoryRecommendation()
    {
        var recommendation = AdaptiveRuntimePolicyAdvisor.RecommendQaPolicy(
            "Create a visual app",
            Array.Empty<AdaptiveRuntimeExecutionReport>());
        AssertTrue(recommendation is null, "No history should not force a recommendation.");
    }

    private void TestVisualPreflightFailureRecommendation()
    {
        var goal = "Create visual app transform";
        var fp = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint(goal);
        var reports = new[]
        {
            new AdaptiveRuntimeExecutionReport
            {
                GoalFingerprint = fp,
                Success = false,
                FailureStage = "preflight",
                RunVisualQa = true,
                ResolvedQaPolicy = "prod"
            },
            new AdaptiveRuntimeExecutionReport
            {
                GoalFingerprint = fp,
                Success = false,
                FailureStage = "preflight",
                RunVisualQa = true,
                ResolvedQaPolicy = "prod"
            }
        };

        var recommendation = AdaptiveRuntimePolicyAdvisor.RecommendQaPolicy(goal, reports);
        AssertNotNull(recommendation, "Expected recommendation for repeated preflight visual failures.");
        AssertEqual("demo", recommendation!.QaPolicy);
    }

    private void TestSelfExtendFailureRecommendation()
    {
        var goal = "Generate stable feature extension";
        var fp = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint(goal);
        var reports = new[]
        {
            new AdaptiveRuntimeExecutionReport
            {
                GoalFingerprint = fp,
                Success = false,
                FailureStage = "self-extend",
                ResolvedQaPolicy = "demo"
            },
            new AdaptiveRuntimeExecutionReport
            {
                GoalFingerprint = fp,
                Success = false,
                FailureStage = "self-extend",
                ResolvedQaPolicy = "demo"
            }
        };

        var recommendation = AdaptiveRuntimePolicyAdvisor.RecommendQaPolicy(goal, reports);
        AssertNotNull(recommendation, "Expected recommendation for repeated self-extend failures.");
        AssertEqual("research", recommendation!.QaPolicy);
    }

    private void TestHistoryStoreRoundTrip()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "nexo-runtime-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var report = new AdaptiveRuntimeExecutionReport
            {
                GoalFingerprint = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint("demo goal"),
                GoalPreview = "demo goal",
                Success = true,
                FailureStage = "none",
                ResolvedQaPolicy = "demo"
            };

            AdaptiveRuntimeExecutionHistoryStore.Append(tempRoot, report);
            var loaded = AdaptiveRuntimeExecutionHistoryStore.ReadRecent(tempRoot, maxItems: 10);
            AssertEqual(1, loaded.Count);
            AssertEqual(report.GoalFingerprint, loaded[0].GoalFingerprint);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
