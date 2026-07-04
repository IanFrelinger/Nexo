using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for adaptive runtime policy advisor.</summary>
public sealed class AdaptiveRuntimePolicyAdvisorTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test no history recommendation.</summary>
            TestNoHistoryRecommendation();
            /// <summary>Test visual preflight failure recommendation.</summary>
            TestVisualPreflightFailureRecommendation();
            /// <summary>Test self extend failure recommendation.</summary>
            TestSelfExtendFailureRecommendation();
            /// <summary>Test history store round trip.</summary>
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
        /// <summary>Assert true.</summary>
        /// <param name="null">Null.</param>
        /// <param name="recommendation."">Recommendation.".</param>
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
        /// <summary>Assert not null.</summary>
        /// <param name="failures."">Failures.".</param>
        AssertNotNull(recommendation, "Expected recommendation for repeated preflight visual failures.");
        /// <summary>Assert equal.</summary>
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
        /// <summary>Assert not null.</summary>
        /// <param name="failures."">Failures.".</param>
        AssertNotNull(recommendation, "Expected recommendation for repeated self-extend failures.");
        /// <summary>Assert equal.</summary>
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
            /// <summary>Assert equal.</summary>
            AssertEqual(1, loaded.Count);
            /// <summary>Assert equal.</summary>
            AssertEqual(report.GoalFingerprint, loaded[0].GoalFingerprint);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
