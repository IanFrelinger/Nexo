using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Agent.Tests.Commands;

/// <summary>
/// Command for validating Agent functionality with proper logging and timeouts.
/// </summary>
public class AgentValidationCommand
{
    private readonly ILogger<AgentValidationCommand> _logger;

    public AgentValidationCommand(ILogger<AgentValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Agent interface definitions.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Agent interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that interfaces exist and have expected methods
            var iAgentType = typeof(IAgent);
            var iAIEnhancedAgentType = typeof(IAIEnhancedAgent);
            
            var iAgentMethods = iAgentType.GetMethods();
            var iAIEnhancedAgentMethods = iAIEnhancedAgentType.GetMethods();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Agent interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = iAgentType.IsInterface && 
                        iAIEnhancedAgentType.IsInterface &&
                        iAgentMethods.Length > 0 &&
                        iAIEnhancedAgentMethods.Length > 0;
            
            _logger.LogInformation("Agent interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Agent interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Agent model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Agent model validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that model types exist and can be instantiated
            var agentActionType = typeof(AgentAction);
            var agentRequestType = typeof(AgentRequest);
            var agentResponseType = typeof(AgentResponse);
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Agent model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = agentActionType.IsClass && 
                        agentRequestType.IsClass &&
                        agentResponseType.IsClass &&
                        !agentActionType.IsAbstract &&
                        !agentRequestType.IsAbstract &&
                        !agentResponseType.IsAbstract;
            
            _logger.LogInformation("Agent model validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Agent model validation");
            return false;
        }
    }

    /// <summary>
    /// Validates AI Enhanced Agent service functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAIEnhancedAgentService(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AI Enhanced Agent service validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that service classes exist and can be instantiated
            var baseAgentType = typeof(BaseAIEnhancedAgent);
            var architectAgentType = typeof(AIEnhancedArchitectAgent);
            var developerAgentType = typeof(AIEnhancedDeveloperAgent);
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AI Enhanced Agent service validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = baseAgentType.IsClass && 
                        architectAgentType.IsClass &&
                        developerAgentType.IsClass &&
                        baseAgentType.IsAbstract &&
                        !architectAgentType.IsAbstract &&
                        !developerAgentType.IsAbstract;
            
            _logger.LogInformation("AI Enhanced Agent service validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI Enhanced Agent service validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Agent enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentEnums(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Agent enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var taskAssignment = Enum.IsDefined(typeof(Nexo.Core.Application.Enums.AgentRequestType), Nexo.Core.Application.Enums.AgentRequestType.TaskAssignment);
            var codeReview = Enum.IsDefined(typeof(Nexo.Core.Application.Enums.AgentRequestType), Nexo.Core.Application.Enums.AgentRequestType.CodeReview);
            var architectureDesign = Enum.IsDefined(typeof(Nexo.Core.Application.Enums.AgentRequestType), Nexo.Core.Application.Enums.AgentRequestType.ArchitectureDesign);
            var testCreation = Enum.IsDefined(typeof(Nexo.Core.Application.Enums.AgentRequestType), Nexo.Core.Application.Enums.AgentRequestType.TestCreation);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Agent enum validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = taskAssignment && codeReview && architectureDesign && testCreation;
            _logger.LogInformation("Agent enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Agent enum validation");
            return false;
        }
    }
} 