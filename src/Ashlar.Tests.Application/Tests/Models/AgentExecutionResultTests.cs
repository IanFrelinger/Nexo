using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Application.Tests.Models;

/// <summary>Tests for agent execution result.</summary>
public class AgentExecutionResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test record equality.</summary>
            TestRecordEquality();
            /// <summary>Test record inequality.</summary>
            TestRecordInequality();
            /// <summary>Test initialization with all properties.</summary>
            TestInitializationWithAllProperties();
            /// <summary>Test initialization with optional properties.</summary>
            TestInitializationWithOptionalProperties();
            /// <summary>Test default values.</summary>
            TestDefaultValues();

            return Task.FromResult(new TestResult
            {
                Name = nameof(AgentExecutionResultTests),
                Category = "Application",
                Passed = true,
                Message = "All AgentExecutionResult model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AgentExecutionResultTests),
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

        /// <summary>Assert equal.</summary>
        AssertEqual(result1, result2);
        /// <summary>Assert true.</summary>
        /// <param name="result2">Result2.</param>
        AssertTrue(result1 == result2);
        /// <summary>Assert false.</summary>
        /// <param name="result2">Result2.</param>
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
        /// <summary>Assert false.</summary>
        /// <param name="result2">Result2.</param>
        AssertFalse(result1 == result2);
        /// <summary>Assert true.</summary>
        /// <param name="result2">Result2.</param>
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

        /// <summary>Assert equal.</summary>
        AssertEqual("TestAgent", result.AgentName);
        /// <summary>Assert true.</summary>
        AssertTrue(result.Success);
        /// <summary>Assert equal.</summary>
        /// <param name="successful"">Successful".</param>
        AssertEqual("Execution successful", result.Message);
        /// <summary>Assert equal.</summary>
        AssertEqual(executedAt, result.ExecutedAt);
        /// <summary>Assert equal.</summary>
        AssertEqual(duration, result.Duration);
        /// <summary>Assert not null.</summary>
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

        /// <summary>Assert equal.</summary>
        AssertEqual("TestAgent", result.AgentName);
        /// <summary>Assert false.</summary>
        AssertFalse(result.Success);
        /// <summary>Assert equal.</summary>
        /// <param name="failed"">Failed".</param>
        AssertEqual("Execution failed", result.Message);
        /// <summary>Assert equal.</summary>
        AssertEqual(executedAt, result.ExecutedAt);
        /// <summary>Assert null.</summary>
        AssertNull(result.Duration);
        /// <summary>Assert null.</summary>
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
        /// <summary>Assert null.</summary>
        AssertNull(result.Output);
    }
}

