using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Agents;

namespace Nexo.Tests.Domain.Tests.Agents;

/// <summary>
/// Tests for AgentCard domain model.
/// </summary>
public class AgentCardTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAgentCardProperties();
            await TestPlatformConfig();
            await TestAgentMemoryConfig();
            await TestAgentConstraints();
            
            return new TestResult
            {
                TestName = nameof(AgentCardTests),
                Category = "Domain",
                Passed = true,
                Message = "All agent card tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(AgentCardTests),
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
                TestName = nameof(AgentCardTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestAgentCardProperties()
    {
        var agent = new AgentCard
        {
            Id = "test-agent",
            Name = "Test Agent",
            Version = "1.0.0",
            Icon = "🤖",
            Domain = "Security",
            Description = "A test agent",
            Platforms = [Platform.Web, Platform.Api],
            Behaviors = ["security-scan", "vulnerability-analysis"],
            CanCoordinateWith = ["other-agent"],
            CoordinationProtocols = ["request-response"]
        };
        
        AssertEqual("test-agent", agent.Id);
        AssertEqual("Test Agent", agent.Name);
        AssertEqual("Security", agent.Domain);
        AssertEqual(2, agent.Platforms.Count);
        AssertEqual(2, agent.Behaviors.Count);
        AssertEqual(1, agent.CanCoordinateWith.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestPlatformConfig()
    {
        var config = new PlatformConfig
        {
            UiComponent = "SecurityPanel",
            EntryPoint = "/api/security",
            Capabilities = ["scan", "analyze", "report"]
        };
        
        AssertEqual("SecurityPanel", config.UiComponent);
        AssertEqual("/api/security", config.EntryPoint);
        AssertEqual(3, config.Capabilities.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestAgentMemoryConfig()
    {
        var memory = new AgentMemoryConfig
        {
            SemanticCache = true,
            PatternLibrary = true,
            CrossProjectSharing = false
        };
        
        AssertTrue(memory.SemanticCache);
        AssertTrue(memory.PatternLibrary);
        AssertFalse(memory.CrossProjectSharing);
        
        await Task.CompletedTask;
    }
    
    private async Task TestAgentConstraints()
    {
        var constraints = new AgentConstraints
        {
            MaxExecutionTime = TimeSpan.FromMinutes(10),
            MaxConcurrentBehaviors = 5,
            EscalationRules = [
                new EscalationRule("errorCount > 5", "escalate", "admin", "Too many errors")
            ],
            Permissions = ["read", "write", "execute"]
        };
        
        AssertEqual(TimeSpan.FromMinutes(10), constraints.MaxExecutionTime);
        AssertEqual(5, constraints.MaxConcurrentBehaviors);
        AssertEqual(1, constraints.EscalationRules.Count);
        AssertEqual(3, constraints.Permissions.Count);
        AssertEqual("errorCount > 5", constraints.EscalationRules[0].Condition);
        
        await Task.CompletedTask;
    }
}

