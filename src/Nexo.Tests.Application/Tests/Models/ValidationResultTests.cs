using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using ValidationResult = Nexo.Core.Application.Validation.Models.ValidationResult;

namespace Nexo.Tests.Application.Tests.Models;

/// <summary>Tests for validation result.</summary>
public class ValidationResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test record equality.</summary>
            TestRecordEquality();
            /// <summary>Test record inequality.</summary>
            TestRecordInequality();
            /// <summary>Test initialization with test results.</summary>
            TestInitializationWithTestResults();
            /// <summary>Test initialization without test results.</summary>
            TestInitializationWithoutTestResults();
            /// <summary>Test test result equality.</summary>
            TestTestResultEquality();
            /// <summary>Test test result with optional properties.</summary>
            TestTestResultWithOptionalProperties();

            return Task.FromResult(new TestResult
            {
                Name = nameof(ValidationResultTests),
                Category = "Application",
                Passed = true,
                Message = "All ValidationResult model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(ValidationResultTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestRecordEquality()
    {
        var result1 = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0,
            TestResults = null
        };

        var result2 = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0,
            TestResults = null
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
        var result1 = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0
        };

        var result2 = new ValidationResult
        {
            Passed = false,
            Message = "Some tests failed",
            TestsRun = 5,
            TestsPassed = 3,
            TestsFailed = 2
        };

        AssertFalse(result1.Equals(result2));
        /// <summary>Assert false.</summary>
        /// <param name="result2">Result2.</param>
        AssertFalse(result1 == result2);
        /// <summary>Assert true.</summary>
        /// <param name="result2">Result2.</param>
        AssertTrue(result1 != result2);
    }

    private void TestInitializationWithTestResults()
    {
        var testResults = new List<TestResult>
        {
            new TestResult
            {
                Name = "Test1",
                Passed = true,
                Message = "Test passed",
                Category = "Category1"
            },
            new TestResult
            {
                Name = "Test2",
                Passed = false,
                Message = "Test failed",
                Category = "Category1"
            }
        };

        var result = new ValidationResult
        {
            Passed = false,
            Message = "Some tests failed",
            TestsRun = 2,
            TestsPassed = 1,
            TestsFailed = 1,
            TestResults = testResults
        };

        /// <summary>Assert false.</summary>
        AssertFalse(result.Passed);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result.TestsRun);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, result.TestsPassed);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, result.TestsFailed);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.TestResults);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result.TestResults.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("Test1", result.TestResults[0].Name);
        /// <summary>Assert equal.</summary>
        AssertEqual("Test2", result.TestResults[1].Name);
    }

    private void TestInitializationWithoutTestResults()
    {
        var result = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 0,
            TestsPassed = 0,
            TestsFailed = 0,
            TestResults = null
        };

        /// <summary>Assert true.</summary>
        AssertTrue(result.Passed);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.TestsRun);
        /// <summary>Assert null.</summary>
        AssertNull(result.TestResults);
    }

    private void TestTestResultEquality()
    {
        var testResult1 = new TestResult
        {
            Name = "Test1",
            Passed = true,
            Message = "Message",
            Category = "Category1"
        };

        var testResult2 = new TestResult
        {
            Name = "Test1",
            Passed = true,
            Message = "Message",
            Category = "Category1"
        };

        /// <summary>Assert equal.</summary>
        AssertEqual(testResult1, testResult2);
        /// <summary>Assert true.</summary>
        /// <param name="testResult2">Test result2.</param>
        AssertTrue(testResult1 == testResult2);
        AssertEqual(testResult1.GetHashCode(), testResult2.GetHashCode());
    }

    private void TestTestResultWithOptionalProperties()
    {
        // Test with all properties
        var testResult1 = new TestResult
        {
            Name = "Test1",
            Passed = true,
            Message = "Test passed",
            Category = "Category1"
        };

        /// <summary>Assert equal.</summary>
        AssertEqual("Test1", testResult1.Name);
        /// <summary>Assert true.</summary>
        AssertTrue(testResult1.Passed);
        /// <summary>Assert equal.</summary>
        /// <param name="passed"">Passed".</param>
        AssertEqual("Test passed", testResult1.Message);
        /// <summary>Assert equal.</summary>
        AssertEqual("Category1", testResult1.Category);

        // Test with optional properties as null
        var testResult2 = new TestResult
        {
            Name = "Test2",
            Passed = false,
            Message = null,
            Category = null
        };

        /// <summary>Assert equal.</summary>
        AssertEqual("Test2", testResult2.Name);
        /// <summary>Assert false.</summary>
        AssertFalse(testResult2.Passed);
        /// <summary>Assert null.</summary>
        AssertNull(testResult2.Message);
        /// <summary>Assert null.</summary>
        AssertNull(testResult2.Category);
    }
}

