using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Analysis.Rules;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

public class CodeQualityRuleTests : UnitTestBase
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
                Name = nameof(CodeQualityRuleTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All CodeQualityRule tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(CodeQualityRuleTests),
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
                Name = nameof(CodeQualityRuleTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private void TestRuleProperties()
    {
        var mockLogger = new Mock<ILogger<CodeQualityRule>>();
        var rule = new CodeQualityRule(mockLogger.Object);

        AssertEqual("CodeQuality", rule.Name);
        AssertEqual("Analyzes code quality metrics", rule.Description);
    }

    private async Task TestExceptionHandling()
    {
        var mockLogger = new Mock<ILogger<CodeQualityRule>>();
        var rule = new CodeQualityRule(mockLogger.Object);

        // Use a non-existent file - the tool will fail
        var nonExistentFile = new FileInfo(Path.Combine(_tempDir!.FullName, "nonexistent.dll"));

        // The rule should catch exceptions and return empty violations (CodeQualityRule doesn't add error violations)
        var violations = await rule.AnalyzeAsync(nonExistentFile, CancellationToken.None);

        AssertNotNull(violations);
        // CodeQualityRule catches exceptions but doesn't add violations for errors
        // So violations should be empty
        AssertEqual(0, violations.Count);
    }

    private async Task TestWithNonExistentFile()
    {
        var mockLogger = new Mock<ILogger<CodeQualityRule>>();
        var rule = new CodeQualityRule(mockLogger.Object);

        // Test with a file that doesn't exist
        var file = new FileInfo(Path.Combine(_tempDir!.FullName, "test.dll"));

        // Should handle gracefully and return empty violations
        var violations = await rule.AnalyzeAsync(file, CancellationToken.None);

        AssertNotNull(violations);
        // Should not throw, even if file doesn't exist
    }
}

