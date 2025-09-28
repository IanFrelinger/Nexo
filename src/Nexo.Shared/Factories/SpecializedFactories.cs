using System;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;

namespace Nexo.Shared.Factories
{
    /// <summary>
    /// Factory for creating command instances
    /// </summary>
    public static class CommandFactory
    {
        /// <summary>
        /// Creates a command instance
        /// </summary>
        public static TCommand CreateCommand<TCommand, TInput, TOutput>()
            where TCommand : class, ICommand<TInput, TOutput>, new()
        {
            return FactoryManager.Create<TCommand>();
        }

        /// <summary>
        /// Creates a command with dependencies
        /// </summary>
        public static TCommand CreateCommandWithDependencies<TCommand, TInput, TOutput>()
            where TCommand : class, ICommand<TInput, TOutput>
        {
            return FactoryManager.CreateWithDependencies<TCommand>();
        }
    }

    /// <summary>
    /// Factory for creating agent instances
    /// </summary>
    public static class AgentFactory
    {
        /// <summary>
        /// Creates an agent instance
        /// </summary>
        public static TAgent CreateAgent<TAgent>() where TAgent : class, IAgent, new()
        {
            return FactoryManager.Create<TAgent>();
        }

        /// <summary>
        /// Creates an agent with dependencies
        /// </summary>
        public static TAgent CreateAgentWithDependencies<TAgent>() where TAgent : class, IAgent
        {
            return FactoryManager.CreateWithDependencies<TAgent>();
        }
    }
}
