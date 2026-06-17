using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Logic for selecting which implementation to use at runtime.
/// </summary>
public class ImplementationSelector
{
    /// <summary>
    /// Conditions that prefer deterministic execution.
    /// Expressions evaluated against execution context.
    /// </summary>
    public IReadOnlyList<string> PreferDeterministic { get; init; } = [];
    
    /// <summary>
    /// Conditions that prefer agentic execution.
    /// </summary>
    public IReadOnlyList<string> PreferAgentic { get; init; } = [];
    
    /// <summary>
    /// Default if no conditions match.
    /// </summary>
    public ImplementationType Default { get; init; } = ImplementationType.Deterministic;
    
    /// <summary>
    /// Select implementation based on context.
    /// </summary>
    public ImplementationType Select(IExecutionContext context)
    {
        // Check deterministic conditions
        foreach (var condition in PreferDeterministic)
        {
            if (EvaluateCondition(condition, context))
                return ImplementationType.Deterministic;
        }
        
        // Check agentic conditions
        foreach (var condition in PreferAgentic)
        {
            if (EvaluateCondition(condition, context))
                return ImplementationType.Agentic;
        }
        
        return Default;
    }
    
    private static bool EvaluateCondition(string condition, IExecutionContext context)
    {
        // Simple condition evaluation - extend as needed
        return condition switch
        {
            "environment.airGapped" or "environment.airGapped == true" => context.IsAirGapped,
            "context.auditMode" or "context.auditMode == true" => context.AuditMode,
            _ => false
        };
    }
}
