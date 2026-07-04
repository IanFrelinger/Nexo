using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;

namespace Nexo.Tests.Application.Tests.Models;

/// <summary>Tests for analysis result.</summary>
public class AnalysisResultTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test record equality.</summary>
            TestRecordEquality();
            /// <summary>Test record inequality.</summary>
            TestRecordInequality();
            /// <summary>Test initialization with violations.</summary>
            TestInitializationWithViolations();
            /// <summary>Test initialization without violations.</summary>
            TestInitializationWithoutViolations();
            /// <summary>Test violation record equality.</summary>
            TestViolationRecordEquality();
            /// <summary>Test violation with optional properties.</summary>
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
        /// <summary>Assert false.</summary>
        /// <param name="result2">Result2.</param>
        AssertFalse(result1 == result2);
        /// <summary>Assert true.</summary>
        /// <param name="result2">Result2.</param>
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

        /// <summary>Assert true.</summary>
        AssertTrue(result.HasViolations);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result.TotalViolations);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result.Violations.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("Rule1", result.Violations[0].Rule);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert false.</summary>
        AssertFalse(result.HasViolations);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.TotalViolations);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert equal.</summary>
        AssertEqual(violation1, violation2);
        /// <summary>Assert true.</summary>
        /// <param name="violation2">Violation2.</param>
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

        /// <summary>Assert equal.</summary>
        AssertEqual(10, violation1.LineNumber);
        /// <summary>Assert equal.</summary>
        AssertEqual(RiskLevel.Critical, violation1.Severity);

        // Test with default severity
        var violation2 = new Violation
        {
            Rule = "Rule2",
            Message = "Message",
            FilePath = "file.cs",
            LineNumber = null
        };

        /// <summary>Assert null.</summary>
        AssertNull(violation2.LineNumber);
        /// <summary>Assert equal.</summary>
        AssertEqual(RiskLevel.Medium, violation2.Severity); // Default value
    }
}

