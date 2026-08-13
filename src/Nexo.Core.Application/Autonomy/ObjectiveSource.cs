namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// Where an objective came from (autonomous self-extension spec R1.1), in trust order.
/// The source is recorded on the objective and flows into the certificate's inputs —
/// the loop's decision channel is an attack surface and is treated as one.
/// </summary>
public enum ObjectiveSource
{
    /// <summary>An explicit human objective (trust root; the only source permitted to open Tier 2 work).</summary>
    Human = 0,

    /// <summary>A diagnosed failure class from CI/ledger triage.</summary>
    Triage = 1,

    /// <summary>
    /// An observed workflow gap from watch/adapt telemetry. Untrusted (R1.2): content from
    /// watched workflows never reaches proposer instructions verbatim — objectives from
    /// this source are built by deterministic schema-constrained extraction only.
    /// </summary>
    Telemetry = 2,
}
