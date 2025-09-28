using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Planning
{
    /// <summary>
    /// Execution plan for commands
    /// </summary>
    public class CommandExecutionPlan
    {
        /// <summary>
        /// Commands to be executed
        /// </summary>
        public List<object> Commands { get; set; } = new List<object>();

        /// <summary>
        /// Commands to be executed (typed)
        /// </summary>
        public List<ICommand<object, object>> TypedCommands { get; set; } = new List<ICommand<object, object>>();

        /// <summary>
        /// Order in which commands should be executed
        /// </summary>
        public List<string> ExecutionOrder { get; set; } = new List<string>();

        /// <summary>
        /// Estimated duration of execution
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Dependencies between commands
        /// </summary>
        public Dictionary<string, List<string>> Dependencies { get; set; } = new Dictionary<string, List<string>>();
    }
}
