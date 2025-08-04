using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Domain.Tests.Commands;
using Nexo.Core.Domain.Enums;
using System;

namespace Nexo.Core.Domain.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Core.Domain layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class CoreDomainPipelineTests
{
    private readonly ILogger<CoreDomainPipelineTests> _logger;

    public CoreDomainPipelineTests()
    {
        _logger = NullLogger<CoreDomainPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Agent_Entity_WorksCorrectly()
    {
        _logger.LogInformation("Starting Agent entity test");
        
        var command = new EntityValidationCommand(NullLogger<EntityValidationCommand>.Instance);
        var result = command.ValidateAgent(timeoutMs: 5000);
        
        Assert.True(result, "Agent entity should work correctly");
        _logger.LogInformation("Agent entity test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Project_Entity_WorksCorrectly()
    {
        _logger.LogInformation("Starting Project entity test");
        
        var command = new EntityValidationCommand(NullLogger<EntityValidationCommand>.Instance);
        var result = command.ValidateProject(timeoutMs: 5000);
        
        Assert.True(result, "Project entity should work correctly");
        _logger.LogInformation("Project entity test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Sprint_Entity_WorksCorrectly()
    {
        _logger.LogInformation("Starting Sprint entity test");
        
        var command = new EntityValidationCommand(NullLogger<EntityValidationCommand>.Instance);
        var result = command.ValidateSprint(timeoutMs: 5000);
        
        Assert.True(result, "Sprint entity should work correctly");
        _logger.LogInformation("Sprint entity test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void SprintTask_Entity_WorksCorrectly()
    {
        _logger.LogInformation("Starting SprintTask entity test");
        
        var command = new EntityValidationCommand(NullLogger<EntityValidationCommand>.Instance);
        var result = command.ValidateSprintTask(timeoutMs: 5000);
        
        Assert.True(result, "SprintTask entity should work correctly");
        _logger.LogInformation("SprintTask entity test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AgentId_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting AgentId value object test");
        
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var result = command.ValidateAgentId(timeoutMs: 5000);
        
        Assert.True(result, "AgentId value object should work correctly");
        _logger.LogInformation("AgentId value object test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AgentName_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting AgentName value object test");
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var agentName1 = new Nexo.Core.Domain.ValueObjects.AgentName("Agent1");
        var agentName2 = new Nexo.Core.Domain.ValueObjects.AgentName("Agent2");
        var agentName3 = new Nexo.Core.Domain.ValueObjects.AgentName("Agent1");
        Assert.Equal("Agent1", agentName1.Value);
        Assert.Equal("Agent2", agentName2.Value);
        Assert.Equal("Agent1", agentName3.Value);
        Assert.True(agentName1 == agentName3, $"agentName1 == agentName3 failed: {agentName1.Value} vs {agentName3.Value}");
        Assert.True(agentName1 != agentName2, $"agentName1 != agentName2 failed: {agentName1.Value} vs {agentName2.Value}");
        Assert.True(agentName1.Equals(agentName3), $"agentName1.Equals(agentName3) failed: {agentName1.Value} vs {agentName3.Value}");
        Assert.False(agentName1.Equals(agentName2), $"agentName1.Equals(agentName2) failed: {agentName1.Value} vs {agentName2.Value}");
        _logger.LogInformation("AgentName value object test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AgentRole_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting AgentRole value object test");
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var agentRole1 = new Nexo.Core.Domain.ValueObjects.AgentRole("Developer");
        var agentRole2 = new Nexo.Core.Domain.ValueObjects.AgentRole("Architect");
        var agentRole3 = new Nexo.Core.Domain.ValueObjects.AgentRole("Developer");
        Assert.Equal("Developer", agentRole1.Value);
        Assert.Equal("Architect", agentRole2.Value);
        Assert.Equal("Developer", agentRole3.Value);
        Assert.True(agentRole1 == agentRole3, $"agentRole1 == agentRole3 failed: {agentRole1.Value} vs {agentRole3.Value}");
        Assert.True(agentRole1 != agentRole2, $"agentRole1 != agentRole2 failed: {agentRole1.Value} vs {agentRole2.Value}");
        Assert.True(agentRole1.Equals(agentRole3), $"agentRole1.Equals(agentRole3) failed: {agentRole1.Value} vs {agentRole3.Value}");
        Assert.False(agentRole1.Equals(agentRole2), $"agentRole1.Equals(agentRole2) failed: {agentRole1.Value} vs {agentRole2.Value}");
        _logger.LogInformation("AgentRole value object test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ProjectId_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting ProjectId value object test");
        
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var result = command.ValidateProjectId(timeoutMs: 5000);
        
        Assert.True(result, "ProjectId value object should work correctly");
        _logger.LogInformation("ProjectId value object test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ProjectName_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting ProjectName value object test");
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var projectName1 = new Nexo.Core.Domain.ValueObjects.ProjectName("Project1");
        var projectName2 = new Nexo.Core.Domain.ValueObjects.ProjectName("Project2");
        var projectName3 = new Nexo.Core.Domain.ValueObjects.ProjectName("Project1");
        Assert.Equal("Project1", projectName1.Value);
        Assert.Equal("Project2", projectName2.Value);
        Assert.Equal("Project1", projectName3.Value);
        Assert.True(projectName1 == projectName3, $"projectName1 == projectName3 failed: {projectName1.Value} vs {projectName3.Value}");
        Assert.True(projectName1 != projectName2, $"projectName1 != projectName2 failed: {projectName1.Value} vs {projectName2.Value}");
        Assert.True(projectName1.Equals(projectName3), $"projectName1.Equals(projectName3) failed: {projectName1.Value} vs {projectName3.Value}");
        Assert.False(projectName1.Equals(projectName2), $"projectName1.Equals(projectName2) failed: {projectName1.Value} vs {projectName2.Value}");
        _logger.LogInformation("ProjectName value object test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void ProjectPath_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting ProjectPath value object test");
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var projectPath1 = new Nexo.Core.Domain.ValueObjects.ProjectPath("/path/to/project1");
        var projectPath2 = new Nexo.Core.Domain.ValueObjects.ProjectPath("/path/to/project2");
        var projectPath3 = new Nexo.Core.Domain.ValueObjects.ProjectPath("/path/to/project1");
        Assert.Equal("/path/to/project1", projectPath1.Value);
        Assert.Equal("/path/to/project2", projectPath2.Value);
        Assert.Equal("/path/to/project1", projectPath3.Value);
        Assert.True(projectPath1 == projectPath3, $"projectPath1 == projectPath3 failed: {projectPath1.Value} vs {projectPath3.Value}");
        Assert.True(projectPath1 != projectPath2, $"projectPath1 != projectPath2 failed: {projectPath1.Value} vs {projectPath2.Value}");
        Assert.True(projectPath1.Equals(projectPath3), $"projectPath1.Equals(projectPath3) failed: {projectPath1.Value} vs {projectPath3.Value}");
        Assert.False(projectPath1.Equals(projectPath2), $"projectPath1.Equals(projectPath2) failed: {projectPath1.Value} vs {projectPath2.Value}");
        _logger.LogInformation("ProjectPath value object test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void SprintId_ValueObject_WorksCorrectly()
    {
        _logger.LogInformation("Starting SprintId value object test");
        
        var command = new ValueObjectValidationCommand(NullLogger<ValueObjectValidationCommand>.Instance);
        var result = command.ValidateSprintId(timeoutMs: 5000);
        
        Assert.True(result, "SprintId value object should work correctly");
        _logger.LogInformation("SprintId value object test completed successfully");
    }

    [Fact(Timeout = 5000)]
    public void Enums_Values_AreDefined()
    {
        _logger.LogInformation("Starting enum values test");
        
        // Test that enum values are accessible
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Inactive));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Active));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Busy));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Failed));
        
        Assert.True(Enum.IsDefined(typeof(ProjectStatus), ProjectStatus.NotInitialized));
        Assert.True(Enum.IsDefined(typeof(ProjectStatus), ProjectStatus.Initialized));
        Assert.True(Enum.IsDefined(typeof(ProjectStatus), ProjectStatus.Running));
        Assert.True(Enum.IsDefined(typeof(ProjectStatus), ProjectStatus.Stopped));
        Assert.True(Enum.IsDefined(typeof(ProjectStatus), ProjectStatus.Failed));
        
        Assert.True(Enum.IsDefined(typeof(SprintStatus), SprintStatus.Planning));
        Assert.True(Enum.IsDefined(typeof(SprintStatus), SprintStatus.Active));
        Assert.True(Enum.IsDefined(typeof(SprintStatus), SprintStatus.Closed));
        
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Domain.Enums.TaskStatus), Nexo.Core.Domain.Enums.TaskStatus.Todo));
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Domain.Enums.TaskStatus), Nexo.Core.Domain.Enums.TaskStatus.InProgress));
        Assert.True(Enum.IsDefined(typeof(Nexo.Core.Domain.Enums.TaskStatus), Nexo.Core.Domain.Enums.TaskStatus.Done));
        
        Assert.True(Enum.IsDefined(typeof(TaskPriority), TaskPriority.Low));
        Assert.True(Enum.IsDefined(typeof(TaskPriority), TaskPriority.Medium));
        Assert.True(Enum.IsDefined(typeof(TaskPriority), TaskPriority.High));
        Assert.True(Enum.IsDefined(typeof(TaskPriority), TaskPriority.Critical));
        
        _logger.LogInformation("Enum values test completed successfully");
    }
} 