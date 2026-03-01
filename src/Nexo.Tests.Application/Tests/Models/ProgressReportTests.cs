using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Models;

public class ProgressReportTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestRecordEquality();
            TestRecordInequality();
            TestInitializationWithAllProperties();
            TestInitializationWithOptionalProperties();
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

        AssertEqual(report1, report2);
        AssertTrue(report1 == report2);
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
        AssertFalse(report1 == report2);
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

        AssertEqual(75, report.Percentage);
        AssertEqual("Processing items", report.Message);
        AssertEqual(15, report.CurrentStep);
        AssertEqual(20, report.TotalSteps);
        AssertNotNull(report.Metadata);
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

        AssertEqual(25, report.Percentage);
        AssertEqual("Starting", report.Message);
        AssertNull(report.CurrentStep);
        AssertNull(report.TotalSteps);
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

        AssertNotNull(report.Metadata);
        AssertEqual(3, report.Metadata.Count);
        AssertEqual("test.txt", report.Metadata["fileName"]);
        AssertEqual(1024L, report.Metadata["fileSize"]);
        AssertEqual(true, report.Metadata["processed"]);
    }
}

