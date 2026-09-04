namespace Ashlar.Certification.Contracts;

/// <summary>Stable <see cref="CertificationInput.Kind"/> values recorded on a certificate.</summary>
public static class CertificationInputKinds
{
    /// <summary>Witness spec the correctness and determinism legs judged.</summary>
    public const string Witness = "witness";

    /// <summary>SHA-256 of the assembly bytes the certifier compiled, judged, and ships.</summary>
    public const string GateEmittedArtifact = "gate-emitted-artifact";

    /// <summary>Closed-world compilation-options blob used to emit the artifact.</summary>
    public const string CompileOptions = "compile-options";

    /// <summary>Identity of the certifier process, rule set, and compiler ceiling.</summary>
    public const string CertifierIdentity = "certifier-identity";

    /// <summary>IL import-fence inventory applied to the emitted artifact before load.</summary>
    public const string IlImportFence = "il-import-fence";

    /// <summary>Where candidate code executed during certification (<c>gate-emitted</c> or <c>in-process-fixture</c>).</summary>
    public const string ExecutionMode = "execution-mode";
}
