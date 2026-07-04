using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Core.Application.Workflows;

/// <summary>Mutable runtime state shared across nodes in a single workflow execution.</summary>
public class WorkflowExecutionContext
{
    /// <summary>Correlation id for logs and streamed events.</summary>
    public string CorrelationId { get; init; }

    /// <summary>Workflow definition being executed.</summary>
    public WorkflowDefinition Workflow { get; init; }

    /// <summary>Brick execution context propagated into behavior and brick nodes.</summary>
    public IExecutionContext ExecutionContext { get; init; }

    /// <summary>Metrics accumulator updated as nodes complete.</summary>
    public WorkflowMetrics Metrics { get; set; } = new();

    /// <summary>Creates execution context for a workflow run.</summary>
    /// <param name="correlationId">Correlation id for the run.</param>
    /// <param name="workflow">Workflow definition to execute.</param>
    public WorkflowExecutionContext(string correlationId, WorkflowDefinition workflow)
    {
        CorrelationId = correlationId;
        Workflow = workflow;
        ExecutionContext = new SimpleExecutionContext
        {
            AgentId = "workflow",
            BehaviorId = workflow.Id,
            IsAirGapped = false,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
    }
}
