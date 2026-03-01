using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Domain.Tests.Bricks;

/// <summary>
/// Tests for Brick domain model and related components.
/// </summary>
public class BrickTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBrickProperties();
            await TestDomainKnowledge();
            await TestBrickImplementations();
            await TestImplementationSelector();
            await TestBrickInterface();
            
            return new TestResult
            {
                Name = nameof(BrickTests),
                Category = "Domain",
                Passed = true,
                Message = "All brick tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(BrickTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(BrickTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestBrickProperties()
    {
        var mockProviderFactory = new Mock<IProviderFactory>();
        var mockLogger = new Mock<ILogger<TestBrick>>();
        var brick = new TestBrick(mockProviderFactory.Object, mockLogger.Object);
        
        AssertEqual("test-brick", brick.Id);
        AssertEqual("Test Brick", brick.Name);
        AssertEqual("1.0.0", brick.Version);
        AssertEqual("🧪", brick.Icon);
        AssertEqual(BrickCategory.Analysis, brick.Category);
        AssertEqual("A test brick", brick.Description);
        AssertEqual(ImplementationType.Deterministic, brick.DefaultImplementation);
        
        await Task.CompletedTask;
    }
    
    private async Task TestDomainKnowledge()
    {
        var knowledge = new DomainKnowledge
        {
            Standards = ["OWASP Top 10 2023", "CWE Top 25"],
            Rules = [
                new DomainRule("rule-1", "Test rule", pattern: null, severity: "high", cweId: "CWE-79"),
                new DomainRule("rule-2", "Another rule", pattern: "test.*pattern", severity: null, cweId: null)
            ],
            ReferenceData = new Dictionary<string, string>
            {
                ["config"] = "./config.json"
            },
            LearnedPatterns = [
                new LearnedPattern("pattern1", "safe", 0.95, 100)
            ],
            ExecutionCount = 42
        };
        
        AssertEqual(2, knowledge.Standards.Count);
        AssertEqual(2, knowledge.Rules.Count);
        AssertEqual(1, knowledge.ReferenceData.Count);
        AssertEqual(1, knowledge.LearnedPatterns.Count);
        AssertEqual(42L, knowledge.ExecutionCount);
        
        await Task.CompletedTask;
    }
    
    private async Task TestBrickImplementations()
    {
        var implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "det-impl",
                Name = "Deterministic",
                Description = "Rule-based",
                Executor = "RuleExecutor",
                Config = new Dictionary<string, object> { ["timeout"] = "60s" },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "< 1s",
                    Deterministic = true,
                    RequiresNetwork = false,
                    ResourceUsage = ResourceUsage.Low
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "agentic-impl",
                Name = "Agentic",
                Description = "LLM-based",
                Executor = "LLMExecutor",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are a test",
                    Temperature = 0.1,
                    MaxTokens = 2000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "5-15s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.High
                }
            }
        };
        
        AssertTrue(implementations.HasDeterministic);
        AssertTrue(implementations.HasAgentic);
        AssertNotNull(implementations.Deterministic);
        AssertNotNull(implementations.Agentic);
        AssertEqual("det-impl", implementations.Deterministic.Id);
        AssertEqual("agentic-impl", implementations.Agentic.Id);
        
        await Task.CompletedTask;
    }
    
    private async Task TestImplementationSelector()
    {
        var selector = new ImplementationSelector
        {
            PreferDeterministic = ["environment.airGapped", "context.auditMode"],
            PreferAgentic = ["input.language == 'solidity'"],
            Default = ImplementationType.Deterministic
        };
        
        var context = new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "test-agent",
            BehaviorId = "test-behavior",
            IsAirGapped = true,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
        
        var selected = selector.Select(context);
        AssertEqual(ImplementationType.Deterministic, selected);
        
        await Task.CompletedTask;
    }
    
    private async Task TestBrickInterface()
    {
        var iface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("code", "string", "Source code", required: true),
                new BrickInputDefinition("language", "string", "Language", required: false, defaultValue: "csharp")
            ],
            Outputs = [
                new BrickOutputDefinition("result", "string", "Analysis result")
            ]
        };
        
        AssertEqual(2, iface.Inputs.Count);
        AssertEqual(1, iface.Outputs.Count);
        AssertEqual("code", iface.Inputs[0].Name);
        AssertTrue(iface.Inputs[0].Required);
        AssertFalse(iface.Inputs[1].Required);
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test brick implementation for testing.
/// </summary>
public class TestBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    
    public TestBrick(IProviderFactory providerFactory, ILogger<TestBrick> logger)
    {
        _providerFactory = providerFactory;
        
        Id = "test-brick";
        Name = "Test Brick";
        Version = "1.0.0";
        Icon = "🧪";
        Category = BrickCategory.Analysis;
        Description = "A test brick";
        
        DomainKnowledge = new DomainKnowledge
        {
            Standards = ["Test Standard"],
            Rules = [new DomainRule("test-rule", "Test rule")]
        };
        
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("input", "string", required: true)],
            Outputs = [new BrickOutputDefinition("output", "string")]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "test-det",
                Name = "Test Deterministic",
                Description = "Test",
                Executor = "TestExecutor",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
    }
    
    public override async Task<Core.Domain.Execution.BrickOutput> ExecuteAsync(
        Core.Domain.Execution.BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new Core.Domain.Execution.BrickOutput
        {
            ["result"] = "test result",
            Summary = "Test execution completed"
        };
        
        return await Task.FromResult(output);
    }
}

