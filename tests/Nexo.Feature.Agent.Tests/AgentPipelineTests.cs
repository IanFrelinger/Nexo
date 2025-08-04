using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Agent.Tests.Commands;
using Nexo.Core.Application.Enums;
using System;

namespace Nexo.Feature.Agent.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Agent layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class AgentPipelineTests
{
    private readonly ILogger<AgentPipelineTests> _logger;

    public AgentPipelineTests()
    {
        _logger = NullLogger<AgentPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Agent_Interfaces_WorkCorrectly()
    {
        _logger.LogInformation("Starting Agent interfaces test");
        
        var command = new AgentValidationCommand(NullLogger<AgentValidationCommand>.Instance);
        var result = command.ValidateAgentInterfaces(timeoutMs: 5000);
        
        Assert.True(result, "Agent interfaces should work correctly");
        _logger.LogInformation("Agent interfaces test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Agent_Models_WorkCorrectly()
    {
        _logger.LogInformation("Starting Agent models test");
        
        var command = new AgentValidationCommand(NullLogger<AgentValidationCommand>.Instance);
        var result = command.ValidateAgentModels(timeoutMs: 5000);
        
        Assert.True(result, "Agent models should work correctly");
        _logger.LogInformation("Agent models test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AIEnhancedAgent_Service_WorksCorrectly()
    {
        _logger.LogInformation("Starting AI Enhanced Agent service test");
        
        var command = new AgentValidationCommand(NullLogger<AgentValidationCommand>.Instance);
        var result = command.ValidateAIEnhancedAgentService(timeoutMs: 5000);
        
        Assert.True(result, "AI Enhanced Agent service should work correctly");
        _logger.LogInformation("AI Enhanced Agent service test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Agent_Enums_WorkCorrectly()
    {
        _logger.LogInformation("Starting Agent enums test");
        
        var command = new AgentValidationCommand(NullLogger<AgentValidationCommand>.Instance);
        var result = command.ValidateAgentEnums(timeoutMs: 5000);
        
        Assert.True(result, "Agent enums should work correctly");
        _logger.LogInformation("Agent enums test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AgentRequestType_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting AgentRequestType enum validation");
        
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.TaskAssignment));
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.CodeReview));
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.ArchitectureDesign));
        Assert.True(Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.TestCreation));
        
        _logger.LogInformation("AgentRequestType enum validation completed successfully");
    }
} 