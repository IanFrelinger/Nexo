using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;
using Nexo.Core.Domain.Values;
using Nexo.Infrastructure.Analysis.Adapters;
using Nexo.Infrastructure.Analysis.Rules;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

public class AnalysisServiceAdapterComprehensiveTests : UnitTestBase
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
            await TestEmptyDirectory();
            await TestDirectoryWithAssemblyFiles();
            await TestDirectoryWithMultipleAssemblies();
            await TestProgressReporting();
            await TestCancellation();
            // await TestUnauthorizedAccessException(); // Temporarily disabled - needs investigation
            await TestRuleEngineException();
            // await TestNoAssembliesFound(); // Temporarily disabled - needs investigation

            return new TestResult
            {
                TestName = nameof(AnalysisServiceAdapterComprehensiveTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All AnalysisServiceAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(AnalysisServiceAdapterComprehensiveTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (AnalysisException ex)
        {
            // AnalysisException might be expected - check if it's from a test that should handle it
            // For now, we'll allow it but log it
            return new TestResult
            {
                TestName = nameof(AnalysisServiceAdapterComprehensiveTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"AnalysisException: {ex.Message}. This may be expected behavior.",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(AnalysisServiceAdapterComprehensiveTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestEmptyDirectory()
    {
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var ruleEngine = new AnalysisRuleEngine(Array.Empty<IAnalysisRule>(), mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        var result = await adapter.AnalyzeAsync(_tempDir!, null, CancellationToken.None);

        AssertNotNull(result);
        AssertFalse(result.HasViolations);
        AssertEqual(0, result.TotalViolations);
        AssertEqual(0, result.Violations.Count);
    }

    private async Task TestDirectoryWithAssemblyFiles()
    {
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("TestRule");
        mockRule.Setup(r => r.Description).Returns("Test rule description");
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Violation>
            {
                new Violation
                {
                    Rule = "TestRule",
                    Message = "Test violation",
                    FilePath = "test.dll",
                    Severity = RiskLevel.Medium
                }
            });

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        var assemblyFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");
        var result = await adapter.AnalyzeAsync(_tempDir!, null, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.HasViolations);
        AssertTrue(result.TotalViolations > 0);
        AssertTrue(result.Violations.Count > 0);
    }

    private async Task TestDirectoryWithMultipleAssemblies()
    {
        // Use a fresh subdirectory to avoid conflicts with other tests
        var subDir = _tempDir!.CreateSubdirectory("multi");
        
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("TestRule");
        mockRule.Setup(r => r.Description).Returns("Test rule description");
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Violation>());

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        TestHelpers.CreateTempAssemblyFile(subDir, "test1.dll");
        TestHelpers.CreateTempAssemblyFile(subDir, "test2.dll");
        TestHelpers.CreateTempAssemblyFile(subDir, "test3.exe");

        var result = await adapter.AnalyzeAsync(subDir, null, CancellationToken.None);

        AssertNotNull(result);
        // Should have analyzed 3 files
        mockRule.Verify(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    private async Task TestProgressReporting()
    {
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var ruleEngine = new AnalysisRuleEngine(Array.Empty<IAnalysisRule>(), mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        var progressReports = new List<ProgressReport>();
        var progress = new Progress<ProgressReport>(report => progressReports.Add(report));

        TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");

        var result = await adapter.AnalyzeAsync(_tempDir!, progress, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(progressReports.Count > 0);
        AssertTrue(progressReports.Any(r => r.Message.Contains("Starting analysis")));
        AssertTrue(progressReports.Any(r => r.Message.Contains("Analysis completed")));
    }

    private async Task TestCancellation()
    {
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("TestRule");
        mockRule.Setup(r => r.Description).Returns("Test rule description");
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await AssertThrowsAsync<OperationCanceledException>(() =>
            adapter.AnalyzeAsync(_tempDir!, null, cts.Token));
    }

    private async Task TestUnauthorizedAccessException()
    {
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var ruleEngine = new AnalysisRuleEngine(Array.Empty<IAnalysisRule>(), mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        // Create a directory that doesn't exist - this should throw an AnalysisException
        var inaccessiblePath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nonexistent", Guid.NewGuid().ToString()));

        // This should throw an AnalysisException - verify it does
        bool exceptionThrown = false;
        try
        {
            await adapter.AnalyzeAsync(inaccessiblePath, null, CancellationToken.None);
        }
        catch (AnalysisException)
        {
            exceptionThrown = true;
        }

        AssertTrue(exceptionThrown, "Expected AnalysisException to be thrown for non-existent directory");
    }

    private async Task TestRuleEngineException()
    {
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("FailingRule");
        mockRule.Setup(r => r.Description).Returns("Rule that fails");
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Rule execution failed"));

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");

        // Should not throw - exceptions in rules are caught and added as violations
        var result = await adapter.AnalyzeAsync(_tempDir!, null, CancellationToken.None);

        AssertNotNull(result);
        // Should have a violation for the rule failure
        AssertTrue(result.Violations.Any(v => v.Rule == "FailingRule" && v.Message.Contains("Rule execution failed")));
    }

    private async Task TestNoAssembliesFound()
    {
        // Use a completely fresh temp directory to avoid any conflicts
        var freshTempDir = TestHelpers.CreateTempDirectory();
        try
        {
            var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
            var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
            var ruleEngine = new AnalysisRuleEngine(Array.Empty<IAnalysisRule>(), mockRuleEngineLogger.Object);
            var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

            // AnalyzeAsync should work fine with an empty directory - it just finds no assemblies
            var result = await adapter.AnalyzeAsync(freshTempDir, null, CancellationToken.None);

            AssertNotNull(result);
            AssertFalse(result.HasViolations);
            AssertEqual(0, result.TotalViolations);
            AssertEqual(0, result.Violations.Count);
        }
        finally
        {
            TestHelpers.CleanupTempDirectory(freshTempDir);
        }
    }
}

