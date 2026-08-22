using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Application.Tests.Models;

/// <summary>Tests for test result.</summary>
public class TestResultTests : UnitTestBase
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
            /// <summary>Test test execution result equality.</summary>
            TestTestExecutionResultEquality();
            /// <summary>Test test execution result with categories.</summary>
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
        /// <summary>Assert false.</summary>
        /// <param name="result2">Result2.</param>
        AssertFalse(result1 == result2);
        /// <summary>Assert true.</summary>
        /// <param name="result2">Result2.</param>
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

        /// <summary>Assert equal.</summary>
        AssertEqual("Test1", result.TestName);
        /// <summary>Assert equal.</summary>
        AssertEqual("Category1", result.Category);
        /// <summary>Assert false.</summary>
        AssertFalse(result.Passed);
        /// <summary>Assert equal.</summary>
        /// <param name="failed"">Failed".</param>
        AssertEqual("Test failed", result.Message);
        AssertEqual(TimeSpan.FromMilliseconds(150), result.Duration);
        /// <summary>Assert equal.</summary>
        /// <param name="failed"">Failed".</param>
        AssertEqual("Assertion failed", result.ErrorMessage);
        AssertEqual("at Test1()", result.StackTrace);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Metadata);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert equal.</summary>
        AssertEqual("Test1", result.TestName);
        /// <summary>Assert equal.</summary>
        AssertEqual("Category1", result.Category);
        /// <summary>Assert true.</summary>
        AssertTrue(result.Passed);
        /// <summary>Assert null.</summary>
        AssertNull(result.Message);
        /// <summary>Assert equal.</summary>
        AssertEqual(TimeSpan.Zero, result.Duration);
        /// <summary>Assert null.</summary>
        AssertNull(result.ErrorMessage);
        /// <summary>Assert null.</summary>
        AssertNull(result.StackTrace);
        /// <summary>Assert null.</summary>
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

        /// <summary>Assert equal.</summary>
        AssertEqual(executionResult1, executionResult2);
        /// <summary>Assert true.</summary>
        /// <param name="executionResult2">Execution result2.</param>
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

        /// <summary>Assert equal.</summary>
        AssertEqual(2, executionResult.TotalTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, executionResult.PassedTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, executionResult.FailedTests);
        /// <summary>Assert not null.</summary>
        AssertNotNull(executionResult.Categories);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, executionResult.Categories.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("Category1", executionResult.Categories[0]);
        /// <summary>Assert equal.</summary>
        AssertEqual("Category2", executionResult.Categories[1]);
    }
}

