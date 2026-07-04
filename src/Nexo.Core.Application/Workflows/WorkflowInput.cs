namespace Nexo.Core.Application.Workflows;

/// <summary>Runtime parameters supplied when executing a <see cref="WorkflowExecutor"/> workflow.</summary>
public class WorkflowInput
{
    /// <summary>Named parameter values available to input and cluster nodes.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}
