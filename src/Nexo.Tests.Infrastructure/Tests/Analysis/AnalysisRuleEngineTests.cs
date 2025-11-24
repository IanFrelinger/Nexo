using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;
using Nexo.Infrastructure.Analysis.Rules;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Analysis;

public class AnalysisRuleEngineTests : UnitTestBase
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
            await TestEmptyRulesList();
            await TestSingleRuleExecution();
            await TestMultipleRulesExecution();
            await TestRuleExceptionHandling();
            await TestCancellation();
            await TestGetRules();

            return new TestResult
            {
                TestName = nameof(AnalysisRuleEngineTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All AnalysisRuleEngine tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(AnalysisRuleEngineTests),
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
                TestName = nameof(AnalysisRuleEngineTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestEmptyRulesList()
    {
        var mockLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var ruleEngine = new AnalysisRuleEngine(Array.Empty<IAnalysisRule>(), mockLogger.Object);

        var assemblyFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");
        var violations = await ruleEngine.AnalyzeAsync(assemblyFile, CancellationToken.None);

        AssertNotNull(violations);
        AssertEqual(0, violations.Count);
    }

    private async Task TestSingleRuleExecution()
    {
        var mockLogger = new Mock<ILogger<AnalysisRuleEngine>>();
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

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockLogger.Object);
        var assemblyFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");
        var violations = await ruleEngine.AnalyzeAsync(assemblyFile, CancellationToken.None);

        AssertNotNull(violations);
        AssertEqual(1, violations.Count);
        AssertEqual("TestRule", violations[0].Rule);
        AssertEqual("Test violation", violations[0].Message);

        mockRule.Verify(r => r.AnalyzeAsync(assemblyFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestMultipleRulesExecution()
    {
        var mockLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule1 = new Mock<IAnalysisRule>();
        mockRule1.Setup(r => r.Name).Returns("Rule1");
        mockRule1.Setup(r => r.Description).Returns("Rule 1 description");
        mockRule1.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Violation>
            {
                new Violation { Rule = "Rule1", Message = "Violation 1", FilePath = "test.dll", Severity = RiskLevel.Low }
            });

        var mockRule2 = new Mock<IAnalysisRule>();
        mockRule2.Setup(r => r.Name).Returns("Rule2");
        mockRule2.Setup(r => r.Description).Returns("Rule 2 description");
        mockRule2.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Violation>
            {
                new Violation { Rule = "Rule2", Message = "Violation 2", FilePath = "test.dll", Severity = RiskLevel.High }
            });

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule1.Object, mockRule2.Object }, mockLogger.Object);
        var assemblyFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");
        var violations = await ruleEngine.AnalyzeAsync(assemblyFile, CancellationToken.None);

        AssertNotNull(violations);
        AssertEqual(2, violations.Count);
        AssertTrue(violations.Any(v => v.Rule == "Rule1"));
        AssertTrue(violations.Any(v => v.Rule == "Rule2"));

        mockRule1.Verify(r => r.AnalyzeAsync(assemblyFile, It.IsAny<CancellationToken>()), Times.Once);
        mockRule2.Verify(r => r.AnalyzeAsync(assemblyFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestRuleExceptionHandling()
    {
        var mockLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("FailingRule");
        mockRule.Setup(r => r.Description).Returns("Rule that fails");
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Rule execution failed"));

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockLogger.Object);
        var assemblyFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");

        // Should not throw - exceptions are caught and added as violations
        var violations = await ruleEngine.AnalyzeAsync(assemblyFile, CancellationToken.None);

        AssertNotNull(violations);
        AssertEqual(1, violations.Count);
        AssertEqual("FailingRule", violations[0].Rule);
        AssertTrue(violations[0].Message.Contains("Rule execution failed"));
        AssertEqual(RiskLevel.Medium, violations[0].Severity);
    }

    private async Task TestCancellation()
    {
        var mockLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule = new Mock<IAnalysisRule>();
        mockRule.Setup(r => r.Name).Returns("TestRule");
        mockRule.Setup(r => r.Description).Returns("Test rule description");
        // When cancellation is requested, the rule should throw OperationCanceledException
        // The engine will catch it and add it as a violation
        mockRule.Setup(r => r.AnalyzeAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var ruleEngine = new AnalysisRuleEngine(new[] { mockRule.Object }, mockLogger.Object);
        var assemblyFile = TestHelpers.CreateTempAssemblyFile(_tempDir!, "test.dll");

        // The engine catches OperationCanceledException and adds it as a violation
        // So we should get a violation, not an exception
        var violations = await ruleEngine.AnalyzeAsync(assemblyFile, CancellationToken.None);

        AssertNotNull(violations);
        AssertEqual(1, violations.Count);
        AssertEqual("TestRule", violations[0].Rule);
        AssertTrue(violations[0].Message.Contains("Rule execution failed"));
    }

    private Task TestGetRules()
    {
        var mockLogger = new Mock<ILogger<AnalysisRuleEngine>>();
        var mockRule1 = new Mock<IAnalysisRule>();
        mockRule1.Setup(r => r.Name).Returns("Rule1");
        var mockRule2 = new Mock<IAnalysisRule>();
        mockRule2.Setup(r => r.Name).Returns("Rule2");

        var rules = new[] { mockRule1.Object, mockRule2.Object };
        var ruleEngine = new AnalysisRuleEngine(rules, mockLogger.Object);

        var retrievedRules = ruleEngine.GetRules();

        AssertNotNull(retrievedRules);
        AssertEqual(2, retrievedRules.Count);
        AssertTrue(retrievedRules.Any(r => r.Name == "Rule1"));
        AssertTrue(retrievedRules.Any(r => r.Name == "Rule2"));
        
        return Task.CompletedTask;
    }
}

