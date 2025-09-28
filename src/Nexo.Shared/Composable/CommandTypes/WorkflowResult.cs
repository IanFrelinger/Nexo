using System;
using System.Collections.Generic;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Default implementation of workflow result
    /// </summary>
    public class WorkflowResult : IWorkflowResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public IReadOnlyList<ICommandResult> StepResults { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public DateTime CompletedAt { get; }
        public TimeSpan Duration { get; }
        
        public WorkflowResult(
            bool isSuccess,
            string message,
            IReadOnlyList<ICommandResult> stepResults,
            IReadOnlyDictionary<string, object> metadata,
            DateTime completedAt,
            TimeSpan duration)
        {
            IsSuccess = isSuccess;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            StepResults = stepResults ?? throw new ArgumentNullException(nameof(stepResults));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            CompletedAt = completedAt;
            Duration = duration;
        }
        
        public override string ToString() => $"Workflow {(IsSuccess ? "succeeded" : "failed")}: {Message}";
    }
}
