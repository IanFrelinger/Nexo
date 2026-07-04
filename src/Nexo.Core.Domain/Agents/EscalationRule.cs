namespace Nexo.Core.Domain.Agents;

/// <summary>
/// Rule for escalating agent actions.
/// </summary>
public class EscalationRule
{
    /// <summary>Expression evaluated against runtime context to trigger escalation.</summary>
    public string Condition { get; init; } = default!;

    /// <summary>Action to take when <see cref="Condition"/> matches.</summary>
    public string Action { get; init; } = default!;

    /// <summary>Optional target agent, channel, or handler for the action.</summary>
    public string? Target { get; init; }

    /// <summary>Optional human-readable reason recorded in audit logs.</summary>
    public string? Reason { get; init; }

    /// <summary>Defines an escalation rule.</summary>
    /// <param name="condition">Trigger expression.</param>
    /// <param name="action">Escalation action name.</param>
    /// <param name="target">Optional escalation target.</param>
    /// <param name="reason">Optional audit reason.</param>
    public EscalationRule(string condition, string action, string? target = null, string? reason = null)
    {
        Condition = condition;
        Action = action;
        Target = target;
        Reason = reason;
    }
}
