using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using ValidationTestResult = Nexo.Core.Application.Validation.Models.TestResult;
using ValidationResult = Nexo.Core.Application.Validation.Models.ValidationResult;

namespace Nexo.Tests.Application.Tests.Models;

public class ValidationResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestRecordEquality();
            TestRecordInequality();
            TestInitializationWithTestResults();
            TestInitializationWithoutTestResults();
            TestValidationTestResultEquality();
            TestValidationTestResultWithOptionalProperties();

            return Task.FromResult(new TestResult
            {
                TestName = nameof(ValidationResultTests),
                Category = "Application",
                Passed = true,
                Message = "All ValidationResult model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(ValidationResultTests),
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

        AssertEqual(result1, result2);
        AssertTrue(result1 == result2);
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
        AssertFalse(result1 == result2);
        AssertTrue(result1 != result2);
    }

    private void TestInitializationWithTestResults()
    {
        var testResults = new List<ValidationTestResult>
        {
            new ValidationTestResult
            {
                Name = "Test1",
                Passed = true,
                Message = "Test passed",
                Category = "Category1"
            },
            new ValidationTestResult
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

        AssertFalse(result.Passed);
        AssertEqual(2, result.TestsRun);
        AssertEqual(1, result.TestsPassed);
        AssertEqual(1, result.TestsFailed);
        AssertNotNull(result.TestResults);
        AssertEqual(2, result.TestResults.Count);
        AssertEqual("Test1", result.TestResults[0].Name);
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

        AssertTrue(result.Passed);
        AssertEqual(0, result.TestsRun);
        AssertNull(result.TestResults);
    }

    private void TestValidationTestResultEquality()
    {
        var testResult1 = new ValidationTestResult
        {
            Name = "Test1",
            Passed = true,
            Message = "Message",
            Category = "Category1"
        };

        var testResult2 = new ValidationTestResult
        {
            Name = "Test1",
            Passed = true,
            Message = "Message",
            Category = "Category1"
        };

        AssertEqual(testResult1, testResult2);
        AssertTrue(testResult1 == testResult2);
        AssertEqual(testResult1.GetHashCode(), testResult2.GetHashCode());
    }

    private void TestValidationTestResultWithOptionalProperties()
    {
        // Test with all properties
        var testResult1 = new ValidationTestResult
        {
            Name = "Test1",
            Passed = true,
            Message = "Test passed",
            Category = "Category1"
        };

        AssertEqual("Test1", testResult1.Name);
        AssertTrue(testResult1.Passed);
        AssertEqual("Test passed", testResult1.Message);
        AssertEqual("Category1", testResult1.Category);

        // Test with optional properties as null
        var testResult2 = new ValidationTestResult
        {
            Name = "Test2",
            Passed = false,
            Message = null,
            Category = null
        };

        AssertEqual("Test2", testResult2.Name);
        AssertFalse(testResult2.Passed);
        AssertNull(testResult2.Message);
        AssertNull(testResult2.Category);
    }
}

