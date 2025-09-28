using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Validation
{
    /// <summary>
    /// Result of command validation
    /// </summary>
    public class CommandValidationResult
    {
        /// <summary>
        /// Whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Errors found during validation
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Warnings found during validation
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Execution plan for the validated commands
        /// </summary>
        public CommandExecutionPlan ExecutionPlan { get; set; } = new CommandExecutionPlan();
    }
}
