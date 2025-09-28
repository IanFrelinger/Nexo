using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Execution
{
    /// <summary>
    /// Result of command orchestration
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    public class OrchestrationResult<TOutput>
    {
        /// <summary>
        /// Whether the orchestration was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The final result of the orchestration
        /// </summary>
        public TOutput FinalResult { get; set; } = default(TOutput)!;

        /// <summary>
        /// Results from individual commands
        /// </summary>
        public Dictionary<string, CommandResult<TOutput>> CommandResults { get; set; } = new Dictionary<string, CommandResult<TOutput>>();

        /// <summary>
        /// Errors that occurred during orchestration
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Warnings that occurred during orchestration
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Total duration of the orchestration
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Execution plan that was used
        /// </summary>
        public CommandExecutionPlan? ExecutionPlan { get; set; }
    }
}
