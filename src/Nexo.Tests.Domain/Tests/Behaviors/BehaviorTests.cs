using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Tests.Domain.Tests.Behaviors;

/// <summary>
/// Tests for Behavior domain model.
/// </summary>
public class BehaviorTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBehaviorProperties();
            await TestBehaviorSteps();
            await TestFailurePolicies();
            await TestSuccessCriteria();
            await TestBehaviorParameters();
            
            return new TestResult
            {
                TestName = nameof(BehaviorTests),
                Category = "Domain",
                Passed = true,
                Message = "All behavior tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(BehaviorTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(BehaviorTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestBehaviorProperties()
    {
        var behavior = new Behavior
        {
            Id = "test-behavior",
            Name = "Test Behavior",
            Version = "1.0.0",
            Description = "A test behavior",
            OnStepFailure = FailurePolicy.Abort,
            MaxRetries = 3,
            Timeout = TimeSpan.FromMinutes(5)
        };
        
        AssertEqual("test-behavior", behavior.Id);
        AssertEqual("Test Behavior", behavior.Name);
        AssertEqual(FailurePolicy.Abort, behavior.OnStepFailure);
        AssertEqual(3, behavior.MaxRetries);
        AssertEqual(TimeSpan.FromMinutes(5), behavior.Timeout);
        
        await Task.CompletedTask;
    }
    
    private async Task TestBehaviorSteps()
    {
        var step = new BehaviorStep
        {
            Id = "step-1",
            BrickId = "test-brick",
            Implementation = ImplementationType.Auto,
            InputMapping = new Dictionary<string, string>
            {
                ["code"] = "input.code",
                ["language"] = "input.language"
            },
            OutputMapping = new Dictionary<string, string>
            {
                ["result"] = "output.result"
            },
            Condition = "input.language != null"
        };
        
        AssertEqual("step-1", step.Id);
        AssertEqual("test-brick", step.BrickId);
        AssertEqual(ImplementationType.Auto, step.Implementation);
        AssertEqual(2, step.InputMapping.Count);
        AssertEqual(1, step.OutputMapping.Count);
        AssertEqual("input.language != null", step.Condition);
        
        await Task.CompletedTask;
    }
    
    private async Task TestFailurePolicies()
    {
        AssertEqual(FailurePolicy.Abort, FailurePolicy.Abort);
        AssertEqual(FailurePolicy.Continue, FailurePolicy.Continue);
        AssertEqual(FailurePolicy.Retry, FailurePolicy.Retry);
        AssertEqual(FailurePolicy.Fallback, FailurePolicy.Fallback);
        
        await Task.CompletedTask;
    }
    
    private async Task TestSuccessCriteria()
    {
        var criteria = new SuccessCriteria
        {
            AllStepsComplete = true,
            OutputValidation = [
                new ValidationRule("result", "notNull"),
                new ValidationRule("count", "greaterThan", 0)
            ],
            CustomExpression = "output.count > 0"
        };
        
        AssertTrue(criteria.AllStepsComplete);
        AssertNotNull(criteria.OutputValidation);
        AssertEqual(2, criteria.OutputValidation.Count);
        AssertEqual("result", criteria.OutputValidation[0].Field);
        AssertEqual("notNull", criteria.OutputValidation[0].Rule);
        
        await Task.CompletedTask;
    }
    
    private async Task TestBehaviorParameters()
    {
        var param = new BehaviorParameter(
            "code",
            "string",
            required: true,
            defaultValue: null,
            description: "Source code to analyze"
        );
        
        AssertEqual("code", param.Name);
        AssertEqual("string", param.Type);
        AssertTrue(param.Required);
        AssertNull(param.Default);
        AssertEqual("Source code to analyze", param.Description);
        
        await Task.CompletedTask;
    }
}

