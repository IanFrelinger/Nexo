using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Models;

public class TestResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestRecordEquality();
            TestRecordInequality();
            TestInitializationWithAllProperties();
            TestInitializationWithOptionalProperties();
            TestTestExecutionResultEquality();
            TestTestExecutionResultWithCategories();

            return Task.FromResult(new TestResult
            {
                Name = nameof(TestResultTests),
                Category = "Application",
                Passed = true,
                Message = "All TestResult model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(TestResultTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestRecordEquality()
    {
        var result1 = new TestResult
        {
            Name = "Test1",
            Category = "Category1",
            Passed = true,
            Message = "Test passed",
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            StackTrace = null,
            Metadata = null
        };

        var result2 = new TestResult
        {
            Name = "Test1",
            Category = "Category1",
            Passed = true,
            Message = "Test passed",
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            StackTrace = null,
            Metadata = null
        };

        AssertEqual(result1, result2);
        AssertTrue(result1 == result2);
        AssertFalse(result1 != result2);
        AssertEqual(result1.GetHashCode(), result2.GetHashCode());
    }

    private void TestRecordInequality()
    {
        var result1 = new TestResult
        {
            Name = "Test1",
            Category = "Category1",
            Passed = true,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        var result2 = new TestResult
        {
            Name = "Test2",
            Category = "Category2",
            Passed = false,
            Duration = TimeSpan.FromMilliseconds(200)
        };

        AssertFalse(result1.Equals(result2));
        AssertFalse(result1 == result2);
        AssertTrue(result1 != result2);
    }

    private void TestInitializationWithAllProperties()
    {
        var metadata = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        var result = new TestResult
        {
            Name = "Test1",
            Category = "Category1",
            Passed = false,
            Message = "Test failed",
            Duration = TimeSpan.FromMilliseconds(150),
            ErrorMessage = "Assertion failed",
            StackTrace = "at Test1()",
            Metadata = metadata
        };

        AssertEqual("Test1", result.TestName);
        AssertEqual("Category1", result.Category);
        AssertFalse(result.Passed);
        AssertEqual("Test failed", result.Message);
        AssertEqual(TimeSpan.FromMilliseconds(150), result.Duration);
        AssertEqual("Assertion failed", result.ErrorMessage);
        AssertEqual("at Test1()", result.StackTrace);
        AssertNotNull(result.Metadata);
        AssertEqual(2, result.Metadata.Count);
    }

    private void TestInitializationWithOptionalProperties()
    {
        var result = new TestResult
        {
            Name = "Test1",
            Category = "Category1",
            Passed = true,
            Message = null,
            Duration = TimeSpan.Zero,
            ErrorMessage = null,
            StackTrace = null,
            Metadata = null
        };

        AssertEqual("Test1", result.TestName);
        AssertEqual("Category1", result.Category);
        AssertTrue(result.Passed);
        AssertNull(result.Message);
        AssertEqual(TimeSpan.Zero, result.Duration);
        AssertNull(result.ErrorMessage);
        AssertNull(result.StackTrace);
        AssertNull(result.Metadata);
    }

    private void TestTestExecutionResultEquality()
    {
        var testResults = new List<TestResult>
        {
            new TestResult { Name = "Test1", Category = "Cat1", Passed = true, Duration = TimeSpan.Zero },
            new TestResult { Name = "Test2", Category = "Cat1", Passed = true, Duration = TimeSpan.Zero }
        };

        var executionResult1 = new TestExecutionResult
        {
            TotalTests = 2,
            PassedTests = 2,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(200),
            Results = testResults,
            Categories = null
        };

        var executionResult2 = new TestExecutionResult
        {
            TotalTests = 2,
            PassedTests = 2,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(200),
            Results = testResults,
            Categories = null
        };

        AssertEqual(executionResult1, executionResult2);
        AssertTrue(executionResult1 == executionResult2);
        AssertEqual(executionResult1.GetHashCode(), executionResult2.GetHashCode());
    }

    private void TestTestExecutionResultWithCategories()
    {
        var testResults = new List<TestResult>
        {
            new TestResult { Name = "Test1", Category = "Category1", Passed = true, Duration = TimeSpan.Zero },
            new TestResult { Name = "Test2", Category = "Category2", Passed = true, Duration = TimeSpan.Zero }
        };

        var categories = new List<string> { "Category1", "Category2" };

        var executionResult = new TestExecutionResult
        {
            TotalTests = 2,
            PassedTests = 2,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(200),
            Results = testResults,
            Categories = categories
        };

        AssertEqual(2, executionResult.TotalTests);
        AssertEqual(2, executionResult.PassedTests);
        AssertEqual(0, executionResult.FailedTests);
        AssertNotNull(executionResult.Categories);
        AssertEqual(2, executionResult.Categories.Count);
        AssertEqual("Category1", executionResult.Categories[0]);
        AssertEqual("Category2", executionResult.Categories[1]);
    }
}

