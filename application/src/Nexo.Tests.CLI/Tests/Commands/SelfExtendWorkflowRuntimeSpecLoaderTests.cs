using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public sealed class SelfExtendWorkflowRuntimeSpecLoaderTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestLoadDefault();
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
        AssertEqual("balanced", spec.Workflow.Focus);
        AssertEqual(3, spec.Workflow.MaxIterations);
        AssertTrue(spec.Workflow.RunFunctionalQa, "Expected functional QA enabled by default.");
        AssertTrue(spec.Workflow.RunAestheticQa, "Expected aesthetic QA enabled by default.");
        AssertEqual("strict", spec.Workflow.VisualQaFallbackPolicy);
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
        AssertEqual("aesthetic", spec.Workflow.Focus);
        AssertEqual(5, spec.Workflow.MaxIterations);
        AssertFalse(spec.Workflow.StopOnFirstPass);
        AssertTrue(spec.Workflow.RunVisualQa, "Expected visual QA flag to round-trip from JSON.");
        AssertEqual("degrade", spec.Workflow.VisualQaFallbackPolicy);
        AssertFalse(spec.Workflow.RequirePreflight);
        AssertEqual(3, spec.Workflow.AgentPhases.Length);
    }
}
