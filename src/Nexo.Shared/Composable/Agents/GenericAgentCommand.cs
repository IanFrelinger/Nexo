using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Generic composable command for any AI agent
    /// </summary>
    public class GenericAgentCommand : BaseAgentComposableCommand
    {
        public GenericAgentCommand(
            IAgent agent,
            AgentTask agentTask,
            AgentContext agentContext,
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) 
            : base(agent, agentTask, agentContext, collectionOps, loopOps, stringOps)
        {
        }
        
        public override ICommandIdentifier Id => new CommandIdentifier($"GenericAgent_{Agent.Id}", "Nexo.Agents", "1.0.0");
        public override ICommandName Name => new CommandName($"GenericAgent_{Agent.Name}", $"Generic Agent: {Agent.Name}", "AI Agents");
        public override ICommandDescription Description => new CommandDescription(
            $"Generic AI agent command for {Agent.Name}",
            $"This is a generic command wrapper for the {Agent.Name} agent",
            new[] { "ai", "agent", "generic" },
            new[] { $"Execute {Agent.Name} task" });
        public override ICommandStatus Status => CommandStatus.Ready();
        public override ICommandDependencies Dependencies => CommandDependencies.None();
        public override ICommandCapabilities Capabilities => CommandCapabilities.WithCapabilities(
            new Capability("GenericAgent", $"Generic agent capabilities for {Agent.Name}", "AI", 5));
        
        public override async Task<ICommandResult> ExecuteWithAgentAsync()
        {
            try
            {
                // Execute using the generic agent
                var result = await Agent.ExecuteAsync(AgentTask);
                
                if (result.IsSuccess)
                {
                    var metadata = new Dictionary<string, object>
                    {
                        ["AgentType"] = Agent.GetType().Name,
                        ["AgentCapabilities"] = Agent.Capabilities.Count,
                        ["AgentPlatform"] = Agent.Platform.ToString()
                    };
                    return CreateSuccessResult("Generic agent execution completed successfully", result.Data, metadata);
                }
                else
                {
                    return CreateFailureResult($"Generic agent execution failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Generic agent execution failed: {ex.Message}");
            }
        }
        
        public override async Task<ICommandResult> ValidateWithAgentAsync()
        {
            try
            {
                // Basic validation - check if agent can execute the task
                if (AgentTask.CanBeExecutedBy(Agent))
                {
                    return CreateSuccessResult("Generic agent validation passed");
                }
                else
                {
                    return CreateFailureResult("Agent cannot execute this task");
                }
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Generic agent validation failed: {ex.Message}");
            }
        }
    }
}
