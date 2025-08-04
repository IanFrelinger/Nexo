using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.ValueObjects;
using System;

namespace Nexo.Core.Domain.Tests.Commands;

/// <summary>
/// Command for validating Core.Domain value objects with proper logging and timeouts.
/// </summary>
public class ValueObjectValidationCommand
{
    private readonly ILogger<ValueObjectValidationCommand> _logger;

    public ValueObjectValidationCommand(ILogger<ValueObjectValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates AgentId value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentId(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AgentId validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var agentId1 = AgentId.New();
            var agentId2 = AgentId.New();
            var agentId3 = AgentId.New();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AgentId validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = agentId1 != agentId2 && 
                        agentId2 != agentId3 && 
                        agentId1 != agentId3 &&
                        !string.IsNullOrEmpty(agentId1.Value) &&
                        !string.IsNullOrEmpty(agentId2.Value) &&
                        !string.IsNullOrEmpty(agentId3.Value);
            
            _logger.LogInformation("AgentId validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AgentId validation");
            return false;
        }
    }

    /// <summary>
    /// Validates AgentName value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentName(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AgentName validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var agentName1 = new AgentName("Agent1");
            var agentName2 = new AgentName("Agent2");
            var agentName3 = new AgentName("Agent1");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AgentName validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = agentName1.Value == "Agent1" && 
                        agentName2.Value == "Agent2" &&
                        agentName3.Value == "Agent1" &&
                        agentName1 == agentName3 &&
                        agentName1 != agentName2;
            
            _logger.LogInformation("AgentName validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AgentName validation");
            return false;
        }
    }

    /// <summary>
    /// Validates AgentRole value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentRole(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AgentRole validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var agentRole1 = new AgentRole("Developer");
            var agentRole2 = new AgentRole("Architect");
            var agentRole3 = new AgentRole("Developer");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AgentRole validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = agentRole1.Value == "Developer" && 
                        agentRole2.Value == "Architect" &&
                        agentRole3.Value == "Developer" &&
                        agentRole1 == agentRole3 &&
                        agentRole1 != agentRole2;
            
            _logger.LogInformation("AgentRole validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AgentRole validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ProjectId value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProjectId(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ProjectId validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var projectId1 = ProjectId.New();
            var projectId2 = ProjectId.New();
            var projectId3 = ProjectId.New();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ProjectId validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = projectId1 != projectId2 && 
                        projectId2 != projectId3 && 
                        projectId1 != projectId3 &&
                        projectId1.Value != Guid.Empty &&
                        projectId2.Value != Guid.Empty &&
                        projectId3.Value != Guid.Empty;
            
            _logger.LogInformation("ProjectId validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProjectId validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ProjectName value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProjectName(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ProjectName validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var projectName1 = new ProjectName("Project1");
            var projectName2 = new ProjectName("Project2");
            var projectName3 = new ProjectName("Project1");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ProjectName validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = projectName1.Value == "Project1" && 
                        projectName2.Value == "Project2" &&
                        projectName3.Value == "Project1" &&
                        projectName1 == projectName3 &&
                        projectName1 != projectName2;
            
            _logger.LogInformation("ProjectName validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProjectName validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ProjectPath value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProjectPath(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ProjectPath validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var projectPath1 = new ProjectPath("/path/to/project1");
            var projectPath2 = new ProjectPath("/path/to/project2");
            var projectPath3 = new ProjectPath("/path/to/project1");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ProjectPath validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = projectPath1.Value == "/path/to/project1" && 
                        projectPath2.Value == "/path/to/project2" &&
                        projectPath3.Value == "/path/to/project1" &&
                        projectPath1 == projectPath3 &&
                        projectPath1 != projectPath2;
            
            _logger.LogInformation("ProjectPath validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProjectPath validation");
            return false;
        }
    }

    /// <summary>
    /// Validates SprintId value object.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateSprintId(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting SprintId validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var sprintId1 = SprintId.New();
            var sprintId2 = SprintId.New();
            var sprintId3 = SprintId.New();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("SprintId validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = sprintId1 != sprintId2 && 
                        sprintId2 != sprintId3 && 
                        sprintId1 != sprintId3 &&
                        sprintId1.Value != Guid.Empty &&
                        sprintId2.Value != Guid.Empty &&
                        sprintId3.Value != Guid.Empty;
            
            _logger.LogInformation("SprintId validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SprintId validation");
            return false;
        }
    }
} 