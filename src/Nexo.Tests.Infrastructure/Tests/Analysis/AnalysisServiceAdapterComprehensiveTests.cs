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
            await TestUnauthorizedAccessException();
            await TestRuleEngineException();
            await TestNoAssembliesFound();

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
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("TestRule");
        mockRule.Setup(r => r.Description).Returns("Test rule description");
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Violation>());

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        TestHelpers.CreateTempAssemblyFile(_tempDir!, "test1.dll");
        TestHelpers.CreateTempAssemblyFile(_tempDir!, "test2.dll");
        TestHelpers.CreateTempAssemblyFile(_tempDir!, "test3.exe");

        var result = await adapter.AnalyzeAsync(_tempDir!, null, CancellationToken.None);

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

        // Create a directory that we can't access (simulated by using a non-existent parent)
        var inaccessiblePath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nonexistent", Guid.NewGuid().ToString()));

        try
        {
            await adapter.AnalyzeAsync(inaccessiblePath, null, CancellationToken.None);
            // If we get here, the test should fail - we expected an exception
            throw new AssertionException("Expected AnalysisException for inaccessible path");
        }
        catch (AnalysisException ex)
        {
            // Expected - verify it has the correct error code
            AssertNotNull(ex.ErrorCode);
            AssertTrue(ex.ErrorCode == ErrorCodes.AnalysisUnauthorizedAccess || ex.Message.Contains("Unauthorized"));
        }
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
        var mockLogger = new Mock<ILogger<AnalysisServiceAdapter>>();
        var mockRuleEngineLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var ruleEngine = new AnalysisRuleEngine(Array.Empty<IAnalysisRule>(), mockRuleEngineLogger.Object);
        var adapter = new AnalysisServiceAdapter(mockLogger.Object, ruleEngine);

        // Create a subdirectory with no assemblies
        var subDir = _tempDir!.CreateSubdirectory("subdir");
        var result = await adapter.AnalyzeAsync(subDir, null, CancellationToken.None);

        AssertNotNull(result);
        AssertFalse(result.HasViolations);
        AssertEqual(0, result.TotalViolations);
    }
}

