using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Models;

public class AgentExecutionResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestRecordEquality();
            TestRecordInequality();
            TestInitializationWithAllProperties();
            TestInitializationWithOptionalProperties();
            TestDefaultValues();

            return Task.FromResult(new TestResult
            {
                TestName = nameof(AgentExecutionResultTests),
                Category = "Application",
                Passed = true,
                Message = "All AgentExecutionResult model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(AgentExecutionResultTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestRecordEquality()
    {
        var executedAt = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(5);

        var result1 = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Success",
            ExecutedAt = executedAt,
            Duration = duration,
            Output = null
        };

        var result2 = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Success",
            ExecutedAt = executedAt,
            Duration = duration,
            Output = null
        };

        AssertEqual(result1, result2);
        AssertTrue(result1 == result2);
        AssertFalse(result1 != result2);
        AssertEqual(result1.GetHashCode(), result2.GetHashCode());
    }

    private void TestRecordInequality()
    {
        var executedAt = DateTime.UtcNow;

        var result1 = new AgentExecutionResult
        {
            AgentName = "TestAgent1",
            Success = true,
            Message = "Success",
            ExecutedAt = executedAt
        };

        var result2 = new AgentExecutionResult
        {
            AgentName = "TestAgent2",
            Success = false,
            Message = "Failed",
            ExecutedAt = executedAt
        };

        AssertFalse(result1.Equals(result2));
        AssertFalse(result1 == result2);
        AssertTrue(result1 != result2);
    }

    private void TestInitializationWithAllProperties()
    {
        var executedAt = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(10);
        var output = new { Result = "Test output" };

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Execution successful",
            ExecutedAt = executedAt,
            Duration = duration,
            Output = output
        };

        AssertEqual("TestAgent", result.AgentName);
        AssertTrue(result.Success);
        AssertEqual("Execution successful", result.Message);
        AssertEqual(executedAt, result.ExecutedAt);
        AssertEqual(duration, result.Duration);
        AssertNotNull(result.Output);
    }

    private void TestInitializationWithOptionalProperties()
    {
        var executedAt = DateTime.UtcNow;

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = false,
            Message = "Execution failed",
            ExecutedAt = executedAt,
            Duration = null,
            Output = null
        };

        AssertEqual("TestAgent", result.AgentName);
        AssertFalse(result.Success);
        AssertEqual("Execution failed", result.Message);
        AssertEqual(executedAt, result.ExecutedAt);
        AssertNull(result.Duration);
        AssertNull(result.Output);
    }

    private void TestDefaultValues()
    {
        var executedAt = DateTime.UtcNow;

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Success",
            ExecutedAt = executedAt
        };

        // ExecutedAt should default to DateTime.UtcNow if not set, but we're setting it explicitly
        // Duration and Output should be null by default
        AssertNull(result.Duration);
        AssertNull(result.Output);
    }
}

