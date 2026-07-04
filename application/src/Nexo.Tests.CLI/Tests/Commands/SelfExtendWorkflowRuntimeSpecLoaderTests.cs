using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for self extend workflow runtime spec loader.</summary>
public sealed class SelfExtendWorkflowRuntimeSpecLoaderTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test load default.</summary>
            TestLoadDefault();
            /// <summary>Test load from inline json.</summary>
            TestLoadFromInlineJson();

            return Task.FromResult(new TestResult
            {
                Name = nameof(SelfExtendWorkflowRuntimeSpecLoaderTests),
                Category = "CLI",
                Passed = true,
                Message = "Self-extend workflow runtime spec loader tests passed"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(SelfExtendWorkflowRuntimeSpecLoaderTests),
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
                Name = nameof(SelfExtendWorkflowRuntimeSpecLoaderTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestLoadDefault()
    {
        var spec = SelfExtendWorkflowRuntimeSpecLoader.Load(path: null, json: null);
        /// <summary>Assert equal.</summary>
        AssertEqual("balanced", spec.Workflow.Focus);
        /// <summary>Assert equal.</summary>
        AssertEqual(3, spec.Workflow.MaxIterations);
        /// <summary>Assert true.</summary>
        /// <param name="default."">Default.".</param>
        AssertTrue(spec.Workflow.RunFunctionalQa, "Expected functional QA enabled by default.");
        /// <summary>Assert true.</summary>
        /// <param name="default."">Default.".</param>
        AssertTrue(spec.Workflow.RunAestheticQa, "Expected aesthetic QA enabled by default.");
        /// <summary>Assert equal.</summary>
        AssertEqual("strict", spec.Workflow.VisualQaFallbackPolicy);
        /// <summary>Assert true.</summary>
        /// <param name="default."">Default.".</param>
        AssertTrue(spec.Workflow.RequirePreflight, "Expected preflight required by default.");
    }

    private void TestLoadFromInlineJson()
    {
        const string json = """
{
  "workflow": {
    "focus": "aesthetic",
    "maxIterations": 5,
    "stopOnFirstPass": false,
    "runVisualQa": true,
    "visualQaFallbackPolicy": "degrade",
    "requirePreflight": false,
    "agentPhases": ["planner", "builder", "qa-aesthetic"]
  }
}
""";

        var spec = SelfExtendWorkflowRuntimeSpecLoader.Load(path: null, json: json);
        /// <summary>Assert equal.</summary>
        AssertEqual("aesthetic", spec.Workflow.Focus);
        /// <summary>Assert equal.</summary>
        AssertEqual(5, spec.Workflow.MaxIterations);
        /// <summary>Assert false.</summary>
        AssertFalse(spec.Workflow.StopOnFirstPass);
        /// <summary>Assert true.</summary>
        /// <param name="JSON."">Json.".</param>
        AssertTrue(spec.Workflow.RunVisualQa, "Expected visual QA flag to round-trip from JSON.");
        /// <summary>Assert equal.</summary>
        AssertEqual("degrade", spec.Workflow.VisualQaFallbackPolicy);
        /// <summary>Assert false.</summary>
        AssertFalse(spec.Workflow.RequirePreflight);
        /// <summary>Assert equal.</summary>
        AssertEqual(3, spec.Workflow.AgentPhases.Length);
    }
}
