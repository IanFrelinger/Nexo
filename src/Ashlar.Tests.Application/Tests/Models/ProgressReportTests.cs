using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.Application.Tests.Models;

/// <summary>Tests for progress report.</summary>
public class ProgressReportTests : UnitTestBase
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
            /// <summary>Test initialization with metadata.</summary>
            TestInitializationWithMetadata();

            return Task.FromResult(new TestResult
            {
                Name = nameof(ProgressReportTests),
                Category = "Application",
                Passed = true,
                Message = "All ProgressReport model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(ProgressReportTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestRecordEquality()
    {
        var report1 = new ProgressReport
        {
            Percentage = 50,
            Message = "Processing",
            CurrentStep = 5,
            TotalSteps = 10,
            Metadata = null
        };

        var report2 = new ProgressReport
        {
            Percentage = 50,
            Message = "Processing",
            CurrentStep = 5,
            TotalSteps = 10,
            Metadata = null
        };

        /// <summary>Assert equal.</summary>
        AssertEqual(report1, report2);
        /// <summary>Assert true.</summary>
        /// <param name="report2">Report2.</param>
        AssertTrue(report1 == report2);
        /// <summary>Assert false.</summary>
        /// <param name="report2">Report2.</param>
        AssertFalse(report1 != report2);
        AssertEqual(report1.GetHashCode(), report2.GetHashCode());
    }

    private void TestRecordInequality()
    {
        var report1 = new ProgressReport
        {
            Percentage = 50,
            Message = "Processing"
        };

        var report2 = new ProgressReport
        {
            Percentage = 75,
            Message = "Almost done"
        };

        AssertFalse(report1.Equals(report2));
        /// <summary>Assert false.</summary>
        /// <param name="report2">Report2.</param>
        AssertFalse(report1 == report2);
        /// <summary>Assert true.</summary>
        /// <param name="report2">Report2.</param>
        AssertTrue(report1 != report2);
    }

    private void TestInitializationWithAllProperties()
    {
        var metadata = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        var report = new ProgressReport
        {
            Percentage = 75,
            Message = "Processing items",
            CurrentStep = 15,
            TotalSteps = 20,
            Metadata = metadata
        };

        /// <summary>Assert equal.</summary>
        AssertEqual(75, report.Percentage);
        /// <summary>Assert equal.</summary>
        /// <param name="items"">Items".</param>
        AssertEqual("Processing items", report.Message);
        /// <summary>Assert equal.</summary>
        AssertEqual(15, report.CurrentStep);
        /// <summary>Assert equal.</summary>
        AssertEqual(20, report.TotalSteps);
        /// <summary>Assert not null.</summary>
        AssertNotNull(report.Metadata);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, report.Metadata.Count);
    }

    private void TestInitializationWithOptionalProperties()
    {
        var report = new ProgressReport
        {
            Percentage = 25,
            Message = "Starting",
            CurrentStep = null,
            TotalSteps = null,
            Metadata = null
        };

        /// <summary>Assert equal.</summary>
        AssertEqual(25, report.Percentage);
        /// <summary>Assert equal.</summary>
        AssertEqual("Starting", report.Message);
        /// <summary>Assert null.</summary>
        AssertNull(report.CurrentStep);
        /// <summary>Assert null.</summary>
        AssertNull(report.TotalSteps);
        /// <summary>Assert null.</summary>
        AssertNull(report.Metadata);
    }

    private void TestInitializationWithMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            { "fileName", "test.txt" },
            { "fileSize", 1024L },
            { "processed", true }
        };

        var report = new ProgressReport
        {
            Percentage = 50,
            Message = "Processing file",
            Metadata = metadata
        };

        /// <summary>Assert not null.</summary>
        AssertNotNull(report.Metadata);
        /// <summary>Assert equal.</summary>
        AssertEqual(3, report.Metadata.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("test.txt", report.Metadata["fileName"]);
        /// <summary>Assert equal.</summary>
        AssertEqual(1024L, report.Metadata["fileSize"]);
        /// <summary>Assert equal.</summary>
        AssertEqual(true, report.Metadata["processed"]);
    }
}

