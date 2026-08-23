using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Behaviors;

/// <summary>
/// Validation rule for behavior outputs.
/// </summary>
public class ValidationRule
{
    /// <summary>Output field name to validate.</summary>
    public string Field { get; init; } = default!;

    /// <summary>Validation rule identifier (e.g. <c>notEmpty</c>, <c>regex</c>).</summary>
    public string Rule { get; init; } = default!;

    /// <summary>Optional rule parameter such as a regex pattern or threshold.</summary>
    public object? Value { get; init; }

    /// <summary>Defines an output validation rule.</summary>
    public ValidationRule(string field, string rule, object? value = null)
    {
        Field = field;
        Rule = rule;
        Value = value;
    }
}
