using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Enums;
using System;

namespace Nexo.Core.Domain.Tests.Commands;

/// <summary>
/// Command for validating Core.Domain entities with proper logging and timeouts.
/// </summary>
public class EntityValidationCommand
{
    private readonly ILogger<EntityValidationCommand> _logger;

    public EntityValidationCommand(ILogger<EntityValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Agent entity creation and properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgent(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Agent validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var agentId = AgentId.New();
            var agentName = new AgentName("TestAgent");
            var agentRole = new AgentRole("Developer");
            
            var agent = new Agent(agentId, agentName, agentRole);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Agent validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = agent.Id == agentId && 
                        agent.Name == agentName && 
                        agent.Role == agentRole &&
                        agent.Status == AgentStatus.Inactive &&
                        agent.CreatedAt != default(DateTimeOffset);
            
            _logger.LogInformation("Agent validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Agent validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Project entity creation and properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateProject(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Project validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var projectName = new ProjectName("TestProject");
            var projectPath = new ProjectPath("/path/to/project");
            var containerRuntime = new ContainerRuntime("docker");
            
            var project = new Project(projectName, projectPath, containerRuntime);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Project validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = project.Name == projectName && 
                        project.Path == projectPath &&
                        project.Runtime == containerRuntime &&
                        project.Status == ProjectStatus.NotInitialized &&
                        project.CreatedAt != default(DateTimeOffset);
            
            _logger.LogInformation("Project validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Project validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Sprint entity creation and properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateSprint(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Sprint validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var sprint = new Sprint("Complete feature implementation", 14, 1);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Sprint validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = sprint.Goal == "Complete feature implementation" && 
                        sprint.CapacityDays == 14 &&
                        sprint.SprintNumber == 1 &&
                        sprint.Status == SprintStatus.Planning &&
                        sprint.StartDate != default(DateTimeOffset);
            
            _logger.LogInformation("Sprint validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Sprint validation");
            return false;
        }
    }

    /// <summary>
    /// Validates SprintTask entity creation and properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateSprintTask(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting SprintTask validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var task = new SprintTask("TASK-001", "Implement feature", 5, TaskPriority.Medium);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("SprintTask validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = task.Id == "TASK-001" && 
                        task.Description == "Implement feature" &&
                        task.StoryPoints == 5 &&
                        task.Priority == TaskPriority.Medium &&
                        task.Status == System.Threading.Tasks.TaskStatus.Created;
            
            _logger.LogInformation("SprintTask validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SprintTask validation");
            return false;
        }
    }
} 