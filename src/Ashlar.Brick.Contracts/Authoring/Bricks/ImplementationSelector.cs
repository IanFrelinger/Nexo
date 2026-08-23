using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Bricks;

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
    /// <param name="context">Runtime environment and workflow variables.</param>
    /// <returns>Resolved implementation type.</returns>
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
    
    // Supported condition grammar (evaluated against IExecutionContext):
    //   bare boolean   context.auditMode                       -> truthy value of the path
    //   equality       context.depth == 'deep'                 -> path value equals literal
    //   membership     input.language in ['csharp', 'java']    -> path value is one of the list
    // Paths resolve to IsAirGapped / AuditMode / Provider, else to context.Variables (by full
    // dotted key, then by leaf segment). String comparison is case-insensitive.
    //
    // This used to hardcode only "environment.airGapped" and "context.auditMode": every other
    // authored condition fell through to false, so a brick's real routing silently no-opped —
    // the shipped OWASP scanner's language/depth selectors, for example, never took effect.
    // Genuinely unrecognised grammar still returns false (the selector then falls to Default);
    // it is not treated as an error here because this domain type has no diagnostic channel.
    private static bool EvaluateCondition(string condition, IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return false;
        }

        // No range/index operators below: this project multi-targets netstandard2.0, where
        // System.Index / System.Range are absent.
        var expr = condition.Trim();

        // membership: <path> in ['a', 'b', ...]
        var inIdx = expr.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (inIdx > 0 && expr.EndsWith("]", StringComparison.Ordinal))
        {
            var path = expr.Substring(0, inIdx).Trim();
            var listBody = expr.Substring(inIdx + 4).Trim().TrimStart('[').TrimEnd(']');
            var actual = AsString(Resolve(path, context));
            if (actual is null)
            {
                return false;
            }
            foreach (var raw in listBody.Split(','))
            {
                if (string.Equals(Unquote(raw), actual, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // equality: <path> == <literal>
        var eqIdx = expr.IndexOf("==", StringComparison.Ordinal);
        if (eqIdx > 0)
        {
            var path = expr.Substring(0, eqIdx).Trim();
            var literal = Unquote(expr.Substring(eqIdx + 2).Trim());
            var value = Resolve(path, context);
            if (literal == "true" || literal == "false")
            {
                return AsBool(value) == (literal == "true");
            }
            return string.Equals(AsString(value), literal, StringComparison.OrdinalIgnoreCase);
        }

        // bare boolean: <path>
        return AsBool(Resolve(expr, context));
    }

    private static object? Resolve(string path, IExecutionContext context) => path switch
    {
        "environment.airGapped" => context.IsAirGapped,
        "context.auditMode" => context.AuditMode,
        "environment.provider" or "context.provider" => context.Provider,
        _ => context.Variables.TryGetValue(path, out var v) ? v
            : context.Variables.TryGetValue(Leaf(path), out var leaf) ? leaf
            : null,
    };

    private static string Leaf(string path)
    {
        var dot = path.LastIndexOf('.');
        return dot >= 0 ? path.Substring(dot + 1) : path;
    }

    private static string Unquote(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && ((s[0] == '\'' && s[s.Length - 1] == '\'') || (s[0] == '"' && s[s.Length - 1] == '"')))
        {
            return s.Substring(1, s.Length - 2);
        }
        return s;
    }

    private static string? AsString(object? value) => value switch
    {
        null => null,
        string s => s,
        bool b => b ? "true" : "false",
        _ => value.ToString(),
    };

    private static bool AsBool(object? value) => value switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var b) && b,
        _ => false,
    };
}
