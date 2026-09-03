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
}
