using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// A witness run's verdict: the human-facing failure strings (unchanged, and what the
/// record's reason is built from) plus the same failures as structured findings, so
/// repair feedback can be projected by policy instead of re-parsed from prose.
/// </summary>
internal sealed record WitnessRunResult(
    bool Passed,
    IReadOnlyList<string> Failures,
    IReadOnlyList<WitnessFinding> Findings)
{
    /// <summary>Compatibility overload for callers that only produced strings.</summary>
    public WitnessRunResult(bool passed, IReadOnlyList<string> failures)
        : this(passed, failures, Array.Empty<WitnessFinding>()) { }
}
