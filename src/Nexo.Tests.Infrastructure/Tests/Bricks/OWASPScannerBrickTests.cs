using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Bricks.Owasp.Security;
using ExecutionContext = Nexo.Infrastructure.Execution.ExecutionContext;
using SecurityFinding = Nexo.Bricks.Owasp.Security.SecurityFinding;
using ScanMetadata = Nexo.Bricks.Owasp.Security.ScanMetadata;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Infrastructure.Tests.Bricks;

/// <summary>
/// Tests for OWASPScannerBrick.
/// </summary>
public class OWASPScannerBrickTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBrickProperties();
            await TestDeterministicExecution();
            await TestAgenticExecution();
            await TestDomainKnowledge();
            
            return new TestResult
            {
                Name = nameof(OWASPScannerBrickTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All OWASPScannerBrick tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(OWASPScannerBrickTests),
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
                Name = nameof(OWASPScannerBrickTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestBrickProperties()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockLogger = new Mock<ILogger<OWASPScannerBrick>>();
        var brick = new OWASPScannerBrick(mockProviderFactory.Object, mockLogger.Object);
        
        AssertEqual("owasp-scanner", brick.Id);
        AssertEqual("OWASP Vulnerability Scanner", brick.Name);
        AssertEqual(BrickCategory.Security, brick.Category);
        AssertTrue(brick.Implementations.HasDeterministic);
        AssertTrue(brick.Implementations.HasAgentic);
        AssertEqual(ImplementationType.Deterministic, brick.DefaultImplementation);
        
        await Task.CompletedTask;
    }
    
    private async Task TestDeterministicExecution()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockLogger = new Mock<ILogger<OWASPScannerBrick>>();
        var brick = new OWASPScannerBrick(mockProviderFactory.Object, mockLogger.Object);
        
        var input = new BrickInput();
        input.Set("code", "var query = \"SELECT * FROM users WHERE id = \" + userId;");
        input.Set("language", "csharp");
        
        var context = new ExecutionContext
        {
            AgentId = "test-agent",
            BehaviorId = "test-behavior",
            IsAirGapped = false,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
        
        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context);
        
        AssertNotNull(output);
        AssertNotNull(output.Summary);
        AssertTrue(output.ToDictionary().ContainsKey("findings"));
        AssertTrue(output.ToDictionary().ContainsKey("metadata"));
        
        var findings = output.Get<List<SecurityFinding>>("findings");
        AssertNotNull(findings);
        AssertTrue(findings.Count > 0);
        
        await Task.CompletedTask;
    }
    
    private async Task TestAgenticExecution()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        mockProviderFactory.Setup(f => f.IsProviderAvailable("mock")).Returns(true);
        mockProviderFactory.Setup(f => f.ExecuteLLMAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock LLM response: Found SQL Injection vulnerability");
        
        var mockLogger = new Mock<ILogger<OWASPScannerBrick>>();
        var brick = new OWASPScannerBrick(mockProviderFactory.Object, mockLogger.Object);
        
        var input = new BrickInput();
        input.Set("code", "var query = \"SELECT * FROM users WHERE id = \" + userId;");
        
        var context = new ExecutionContext
        {
            AgentId = "test-agent",
            BehaviorId = "test-behavior",
            IsAirGapped = false,
            AuditMode = false,
            Provider = "mock",
            Variables = new Dictionary<string, object>()
        };
        
        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, context);
        
        AssertNotNull(output);
        AssertNotNull(output.Summary);
        AssertTrue(output.ToDictionary().ContainsKey("findings"));
        
        await Task.CompletedTask;
    }
    
    private async Task TestDomainKnowledge()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockLogger = new Mock<ILogger<OWASPScannerBrick>>();
        var brick = new OWASPScannerBrick(mockProviderFactory.Object, mockLogger.Object);
        
        var knowledge = brick.DomainKnowledge;
        
        AssertTrue(knowledge.Standards.Count > 0);
        AssertTrue(knowledge.Rules.Count > 0);
        AssertTrue(knowledge.LearnedPatterns.Count > 0);
        AssertTrue(knowledge.ReferenceData.Count > 0);
        
        AssertTrue(knowledge.Standards.Contains("OWASP Top 10 2023"));
        AssertTrue(knowledge.Rules.Any(r => r.Id == "sqli-string-concat"));
        
        await Task.CompletedTask;
    }
}
