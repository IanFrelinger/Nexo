using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>
/// One structured finding from a diagnostic probe: the cause plus minimal machine-usable
/// facts. Facts complement — never replace — the verbatim gate feedback (A3): a probe
/// adds structure for the repair loop and the digest, while the raw diagnostics stay
/// untouched on the record's reason.
/// </summary>
public sealed record DiagnosticProbeFinding(
    string Probe,
    string Cause,
    IReadOnlyDictionary<string, string> Facts);

/// <summary>
/// Runs on gate failure and emits cause + minimal facts (trust-loop integration plan G4,
/// the PR-D half left open by the analyzer-gate series). Probes are deterministic reads
/// over the failed decision and its request; they hold no authority — a probe can never
/// change a verdict, and a probe that throws is skipped, logged, and the verdict stands.
/// </summary>
public interface IDiagnosticProbe
{
    /// <summary>The <see cref="CertificationDecision.FailureCheck"/> this probe understands.</summary>
    string FailureCheck { get; }

    /// <summary>Probes a failed decision; null when this failure carries nothing to add.</summary>
    DiagnosticProbeFinding? Probe(CertificationRequest request, CertificationDecision decision);
}
