using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Behaviors;

/// <summary>
/// Criteria for determining behavior success.
/// </summary>
public class SuccessCriteria
{
    /// <summary>Require every non-skipped step to complete successfully.</summary>
    public bool AllStepsComplete { get; init; } = true;

    /// <summary>Optional output field validation rules applied after execution.</summary>
    public IReadOnlyList<ValidationRule>? OutputValidation { get; init; }

    /// <summary>Optional custom expression evaluated to determine success.</summary>
    public string? CustomExpression { get; init; }
}
