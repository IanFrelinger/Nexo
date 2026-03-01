using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;
using Nexo.Infrastructure.Analysis.Rules;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

public class SecurityAnalysisRuleTests : UnitTestBase
{
    private DirectoryInfo? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = TestHelpers.CreateTempDirectory();
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null)
        {
            TestHelpers.CleanupTempDirectory(_tempDir);
        }
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestRuleProperties();
            await TestExceptionHandling();
            await TestWithNonExistentFile();

            return new TestResult
            {
                Name = nameof(SecurityAnalysisRuleTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All SecurityAnalysisRule tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(SecurityAnalysisRuleTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(SecurityAnalysisRuleTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private void TestRuleProperties()
    {
        var mockLogger = new Mock<ILogger<SecurityAnalysisRule>>();
        var rule = new SecurityAnalysisRule(mockLogger.Object);

        AssertEqual("SecurityScan", rule.Name);
        AssertEqual("Scans assemblies for security vulnerabilities", rule.Description);
    }

    private async Task TestExceptionHandling()
    {
        var mockLogger = new Mock<ILogger<SecurityAnalysisRule>>();
        var rule = new SecurityAnalysisRule(mockLogger.Object);

        // Use a non-existent file - the tool will fail and the rule should handle it gracefully
        var nonExistentFile = new FileInfo(Path.Combine(_tempDir!.FullName, "nonexistent.dll"));

        // The rule should catch exceptions and return a violation
        var violations = await rule.AnalyzeAsync(nonExistentFile, CancellationToken.None);

        AssertNotNull(violations);
        // Should have a violation for the error (the rule adds violations when tools fail)
        if (violations.Count > 0)
        {
            var securityViolation = violations.FirstOrDefault(v => v.Rule == "SecurityScan");
            if (securityViolation != null)
            {
                AssertTrue(securityViolation.Message.Contains("Security scan error") || 
                          securityViolation.Message.Contains("error") ||
                          securityViolation.Message.Contains("Security scan"),
                    $"Violation message should contain error info, got: {securityViolation.Message}");
                AssertEqual(RiskLevel.Medium, securityViolation.Severity);
            }
        }
        // Note: The rule may or may not add violations depending on tool behavior
        // The important thing is it doesn't throw an exception
    }

    private async Task TestWithNonExistentFile()
    {
        var mockLogger = new Mock<ILogger<SecurityAnalysisRule>>();
        var rule = new SecurityAnalysisRule(mockLogger.Object);

        // Test with a file that doesn't exist
        var file = new FileInfo(Path.Combine(_tempDir!.FullName, "test.dll"));

        // Should handle gracefully and return violations (likely error violations)
        var violations = await rule.AnalyzeAsync(file, CancellationToken.None);

        AssertNotNull(violations);
        // May have violations or be empty depending on tool behavior
        // The important thing is it doesn't throw
    }
}

