using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>
/// One executable unit in a candidate execution job: the certified candidate itself
/// (<see cref="Assembly"/> null — the backend knows where its built candidate lives) or a
/// compiled mutant (<see cref="Assembly"/> carries the PE bytes).
/// </summary>
/// <param name="UnitId">Caller-chosen id; observations echo it.</param>
/// <param name="Assembly">Mutant PE bytes, or null for the backend's built candidate.</param>
/// <param name="TypeName">Brick type to instantiate.</param>
public sealed record CandidateExecutionUnit(string UnitId, byte[]? Assembly, string TypeName);

/// <summary>
/// A batch of witness executions: every unit runs every witness case <paramref name="Repeats"/>
/// times. Batched so one certification's mutants cost one round trip, not one per mutant.
/// </summary>
/// <param name="Units">Units to execute.</param>
/// <param name="Witness">Witness cases (inputs only are consumed; expectations never leave the gate).</param>
/// <param name="Repeats">Executions per case (2 lets the caller judge determinism).</param>
public sealed record CandidateExecutionJob(
    IReadOnlyList<CandidateExecutionUnit> Units,
    WitnessSpec Witness,
    int Repeats);

/// <summary>
/// What one execution actually did — raw observation, no verdict. The GATE judges;
/// backends only run. A backend that judged its own executions could be lied to once and
/// would then lie to the certificate forever.
/// </summary>
/// <param name="UnitId">Unit this observation belongs to.</param>
/// <param name="CaseIndex">Witness case index.</param>
/// <param name="Repeat">0-based repeat number.</param>
/// <param name="Threw">Whether execution threw (or the unit could not be loaded/instantiated).</param>
/// <param name="Error">Diagnostic when <paramref name="Threw"/>.</param>
/// <param name="Summary">The output's summary string (determinism-relevant).</param>
/// <param name="Outputs">The output dictionary; values may be primitives or JsonElements.</param>
public sealed record CandidateCaseObservation(
    string UnitId,
    int CaseIndex,
    int Repeat,
    bool Threw,
    string? Error,
    string? Summary,
    IReadOnlyDictionary<string, object?>? Outputs);

/// <summary>The observations of one executed job.</summary>
/// <param name="Observations">All observations, unit-major.</param>
public sealed record CandidateExecutionReport(IReadOnlyList<CandidateCaseObservation> Observations);

/// <summary>
/// Executes candidate and mutant code somewhere OTHER than the certifying process — the
/// containment seam for the trust loop's execution legs. When a
/// <see cref="CertificationRequest.ExecutionBackend"/> is present, the certification
/// gate's witness, determinism, and mutation legs route every execution of untrusted
/// candidate code through it and judge the raw observations themselves.
///
/// <para>Fail-closed contract: a backend that cannot execute (infrastructure failure,
/// unparseable results) MUST throw — never fabricate observations. An execution-backend
/// failure is not evidence about the candidate, so it surfaces as an exception, not as a
/// signed FAIL record.</para>
/// </summary>
public interface ICandidateExecutionBackend
{
    /// <summary>Executes the job and returns raw observations.</summary>
    Task<CandidateExecutionReport> ExecuteAsync(CandidateExecutionJob job, CancellationToken cancellationToken = default);

    /// <summary>Stable identity of where execution happens (recorded onto the certificate).</summary>
    string Describe();
}
