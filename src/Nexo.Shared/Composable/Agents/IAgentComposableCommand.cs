using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Interface for AI agent composable commands
    /// </summary>
    public interface IAgentComposableCommand : IComposableCommand
    {
        /// <summary>
        /// The agent that will execute this command
        /// </summary>
        IAgent Agent { get; }
        
        /// <summary>
        /// Agent task that will be executed
        /// </summary>
        AgentTask AgentTask { get; }
        
        /// <summary>
        /// Agent context for execution
        /// </summary>
        AgentContext AgentContext { get; }
        
        /// <summary>
        /// Execute the command using the agent
        /// </summary>
        Task<ICommandResult> ExecuteWithAgentAsync();
        
        /// <summary>
        /// Validate the command using the agent
        /// </summary>
        Task<ICommandResult> ValidateWithAgentAsync();
    }
}
