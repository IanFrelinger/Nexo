using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;

namespace Nexo.Tests.Application.Tests.Models;

public class AnalysisResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestRecordEquality();
            TestRecordInequality();
            TestInitializationWithViolations();
            TestInitializationWithoutViolations();
            TestViolationRecordEquality();
            TestViolationWithOptionalProperties();

            return Task.FromResult(new TestResult
            {
                Name = nameof(AnalysisResultTests),
                Category = "Application",
                Passed = true,
                Message = "All AnalysisResult model tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AnalysisResultTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestRecordEquality()
    {
        var result1 = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        var result2 = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        AssertEqual(result1, result2);
        AssertTrue(result1 == result2);
        AssertFalse(result1 != result2);
        AssertEqual(result1.GetHashCode(), result2.GetHashCode());
    }

    private void TestRecordInequality()
    {
        var result1 = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        var result2 = new AnalysisResult
        {
            HasViolations = true,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        AssertFalse(result1.Equals(result2));
        AssertFalse(result1 == result2);
        AssertTrue(result1 != result2);
    }

    private void TestInitializationWithViolations()
    {
        var violations = new List<Violation>
        {
            new Violation
            {
                Rule = "Rule1",
                Message = "Violation message 1",
                FilePath = "file1.cs",
                LineNumber = 10,
                Severity = RiskLevel.High
            },
            new Violation
            {
                Rule = "Rule2",
                Message = "Violation message 2",
                FilePath = "file2.cs",
                LineNumber = 20,
                Severity = RiskLevel.Critical
            }
        };

        var result = new AnalysisResult
        {
            HasViolations = true,
            Violations = violations,
            TotalViolations = 2
        };

        AssertTrue(result.HasViolations);
        AssertEqual(2, result.TotalViolations);
        AssertEqual(2, result.Violations.Count);
        AssertEqual("Rule1", result.Violations[0].Rule);
        AssertEqual("Rule2", result.Violations[1].Rule);
    }

    private void TestInitializationWithoutViolations()
    {
        var result = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        AssertFalse(result.HasViolations);
        AssertEqual(0, result.TotalViolations);
        AssertEqual(0, result.Violations.Count);
    }

    private void TestViolationRecordEquality()
    {
        var violation1 = new Violation
        {
            Rule = "Rule1",
            Message = "Message",
            FilePath = "file.cs",
            LineNumber = 10,
            Severity = RiskLevel.High
        };

        var violation2 = new Violation
        {
            Rule = "Rule1",
            Message = "Message",
            FilePath = "file.cs",
            LineNumber = 10,
            Severity = RiskLevel.High
        };

        AssertEqual(violation1, violation2);
        AssertTrue(violation1 == violation2);
        AssertEqual(violation1.GetHashCode(), violation2.GetHashCode());
    }

    private void TestViolationWithOptionalProperties()
    {
        // Test with all properties
        var violation1 = new Violation
        {
            Rule = "Rule1",
            Message = "Message",
            FilePath = "file.cs",
            LineNumber = 10,
            Severity = RiskLevel.Critical
        };

        AssertEqual(10, violation1.LineNumber);
        AssertEqual(RiskLevel.Critical, violation1.Severity);

        // Test with default severity
        var violation2 = new Violation
        {
            Rule = "Rule2",
            Message = "Message",
            FilePath = "file.cs",
            LineNumber = null
        };

        AssertNull(violation2.LineNumber);
        AssertEqual(RiskLevel.Medium, violation2.Severity); // Default value
    }
}

