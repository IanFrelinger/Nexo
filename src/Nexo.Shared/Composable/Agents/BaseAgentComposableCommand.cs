using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Base implementation of AI agent composable commands
    /// </summary>
    public abstract class BaseAgentComposableCommand : BaseComposableCommand, IAgentComposableCommand
    {
        public IAgent Agent { get; }
        public AgentTask AgentTask { get; }
        public AgentContext AgentContext { get; }
        
        protected BaseAgentComposableCommand(
            IAgent agent,
            AgentTask agentTask,
            AgentContext agentContext,
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) 
            : base(collectionOps, loopOps, stringOps)
        {
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
            AgentTask = agentTask ?? throw new ArgumentNullException(nameof(agentTask));
            AgentContext = agentContext ?? throw new ArgumentNullException(nameof(agentContext));
        }
        
        public override async Task<ICommandResult> ExecuteAsync(ICommandContext context)
        {
            try
            {
                // Execute using the agent
                var agentResult = await ExecuteWithAgentAsync();
                
                if (agentResult.IsSuccess)
                {
                    return CreateSuccessResult(
                        "Agent command executed successfully",
                        agentResult.Data,
                        new Dictionary<string, object>
                        {
                            ["AgentId"] = Agent.Id.ToString(),
                            ["AgentName"] = Agent.Name.ToString(),
                            ["TaskId"] = AgentTask.Id,
                            ["TaskName"] = AgentTask.Name,
                            ["AgentResult"] = agentResult
                        });
                }
                else
                {
                    return CreateFailureResult(
                        $"Agent command failed: {agentResult.ErrorMessage}",
                        new List<IValidationError>
                        {
                            CreateValidationError("AGENT_EXECUTION_FAILED", agentResult.ErrorMessage, "Agent", "Error")
                        });
                }
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Agent command execution failed: {ex.Message}");
            }
        }
        
        public override async Task<IValidationResult> ValidateAsync(ICommandContext context)
        {
            var errors = new List<IValidationError>();
            var warnings = new List<IValidationWarning>();
            
            try
            {
                // Validate using the agent
                var agentResult = await ValidateWithAgentAsync();
                
                if (!agentResult.IsSuccess)
                {
                    errors.Add(CreateValidationError("AGENT_VALIDATION_FAILED", agentResult.ErrorMessage, "Agent", "Error"));
                }
                
                // Check agent capabilities
                if (!AgentTask.CanBeExecutedBy(Agent))
                {
                    errors.Add(CreateValidationError("INSUFFICIENT_CAPABILITIES", 
                        $"Agent {Agent.Name} does not have the required capabilities for task {AgentTask.Name}", 
                        "Agent", "Error"));
                }
                
                // Check agent status
                if (Agent.Status != "Active")
                {
                    warnings.Add(CreateValidationWarning("AGENT_NOT_ACTIVE", 
                        $"Agent {Agent.Name} is not in active status (current: {Agent.Status})"));
                }
            }
            catch (Exception ex)
            {
                errors.Add(CreateValidationError("VALIDATION_EXCEPTION", ex.Message, "Agent", "Error"));
            }
            
            return new ValidationResult(errors.Count == 0, errors, warnings);
        }
        
        public virtual async Task<ICommandResult> ExecuteWithAgentAsync()
        {
            var agentResult = await Agent.ExecuteAsync(AgentTask);
            if (agentResult.IsSuccess)
            {
                return CreateSuccessResult("Agent execution completed successfully", agentResult.Data);
            }
            else
            {
                return CreateFailureResult($"Agent execution failed: {agentResult.ErrorMessage}");
            }
        }
        
        public virtual async Task<ICommandResult> ValidateWithAgentAsync()
        {
            // Default validation - check if agent can execute the task
            if (AgentTask.CanBeExecutedBy(Agent))
            {
                return CreateSuccessResult("Agent validation passed");
            }
            else
            {
                return CreateFailureResult("Agent cannot execute this task");
            }
        }
        
        public override async Task<ICommandResult> RollbackAsync(ICommandContext context)
        {
            try
            {
                // Agents don't typically support rollback, but we can try to clean up
                await Agent.ShutdownAsync();
                
                return CreateSuccessResult("Agent rollback completed successfully");
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Agent rollback failed: {ex.Message}");
            }
        }
    }
}
