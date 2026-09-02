namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The mutation harness itself failed, so nothing the mutation leg would report is evidence.
///
/// <para>A mutant dies for one of two reasons and they must never be confused: the WITNESS
/// caught it — the only kind of death an escape rate may count — or the HARNESS could not run
/// it. The second proves nothing about the witness, and scoring it as a kill is how a gate ends
/// up signing <c>escape_rate=0</c> for a leg that never ran. The whole certification path
/// therefore treats this as an infrastructure fault and lets it propagate out of
/// <c>CertifyAsync</c>, exactly as an execution-backend failure does: no verdict at all is the
/// correct outcome, in either direction.</para>
/// </summary>
internal sealed class CertificationHarnessException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    public CertificationHarnessException(string message) : base(message) { }

    /// <summary>Creates the exception with an underlying cause.</summary>
    public CertificationHarnessException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// The refusal raised when a compiled mutant carries no <c>CertAuditContext</c>. The
    /// candidate wrapper appends one to every certification-path compile, so its absence means
    /// the wrap did not run or did not run on this text — never that the mutant is dead.
    /// </summary>
    public static CertificationHarnessException MissingAuditContext(string unitId) => new(
        $"Mutation harness cannot run mutant '{unitId}': the compiled assembly carries no "
        + $"'{CandidateSourceWrapper.AuditContextTypeName}' type, so there is no execution context to "
        + "drive the witness with. Every mutant would throw before running a single case and every "
        + "throw would be scored as a kill, which is a mutation verdict with no evidence behind it. "
        + "Fix: compile mutants through CandidateSourceWrapper.Wrap, which appends the audit context "
        + "to every candidate unconditionally. Refusing rather than reporting an unearned escape_rate=0.");
}
