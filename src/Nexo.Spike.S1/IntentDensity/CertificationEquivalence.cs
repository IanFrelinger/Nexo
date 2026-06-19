using Nexo.Spike.S1.Reporting;

namespace Nexo.Spike.S1.IntentDensity;

/// <summary>
/// Capstone property (scoped): within the fixed offline transform catalog and seed range,
/// intent density == 1.0 iff behavioral wrong-impl escape count == 0.
/// </summary>
public static class CertificationEquivalence
{
    public const string ScopeNote =
        "Scoped to the fixed offline transform catalog and configured seed range; " +
        "a lower bound on oracle faithfulness, not universal correctness.";

    public sealed record Result(
        bool DensityEqualsOne,
        bool EscapesEqualsZero,
        bool EquivalenceHolds,
        double IntentDensity,
        int WrongImplEscapes,
        string Scope);

    public static Result Evaluate(IntentDensityReport density, EscapeRateReport escape)
    {
        var densityOne = Math.Abs(density.IntentDensity - 1.0) < 1e-9;
        var escapesZero = escape.WrongImpl.Escapes == 0;
        return new Result(
            densityOne,
            escapesZero,
            densityOne == escapesZero,
            density.IntentDensity,
            escape.WrongImpl.Escapes,
            ScopeNote);
    }

    public static void AssertHolds(IntentDensityReport density, EscapeRateReport escape)
    {
        var result = Evaluate(density, escape);
        if (!result.EquivalenceHolds)
        {
            throw new InvalidOperationException(
                $"Certification equivalence violated: density={result.IntentDensity:P1}, " +
                $"escapes={result.WrongImplEscapes}. {ScopeNote}");
        }
    }
}
