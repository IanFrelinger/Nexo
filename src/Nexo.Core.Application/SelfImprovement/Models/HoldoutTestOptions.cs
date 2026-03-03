namespace Nexo.Core.Application.SelfImprovement.Models;

/// <summary>
/// Options for holdout test set during self-improvement.
/// Holdout tests are excluded from per-fix regression and run only at the end for final validation.
/// P3.4: Prevents overfitting to the training set.
/// </summary>
public sealed class HoldoutTestOptions
{
    /// <summary>
    /// xUnit filter for holdout tests (e.g. "Category=Holdout").
    /// When set, regression during fix validation excludes these tests; they run only at end of loop.
    /// </summary>
    public string? HoldoutFilter { get; set; }

    /// <summary>
    /// When true, run holdout tests at end of self-improvement run and fail if any fail.
    /// </summary>
    public bool ValidateHoldoutAtEnd { get; set; } = true;

    /// <summary>
    /// Derives the regression filter that excludes holdout tests.
    /// For "Category=Holdout", returns "Category!=Holdout".
    /// </summary>
    public string? GetRegressionExclusionFilter()
    {
        if (string.IsNullOrWhiteSpace(HoldoutFilter))
            return null;
        // Support trait-based exclusion: "Category=Holdout" -> "Category!=Holdout"
        var eq = HoldoutFilter!.IndexOf('=');
        if (eq > 0)
        {
            var trait = HoldoutFilter.Substring(0, eq).Trim();
            var value = HoldoutFilter.Substring(eq + 1).Trim();
            return $"{trait}!={value}";
        }
        return null;
    }
}
