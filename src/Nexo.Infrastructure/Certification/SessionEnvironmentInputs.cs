using System.Text.Json;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Projects a sandbox session's environment into certificate <c>inputs</c> (extension spec
/// Part B: image digest, sandbox-spec hash, attestation hash — trust-loop §2.1). Evidence,
/// not a gate: callers append these to <c>CertificationRequest.AdditionalInputs</c> so the
/// certificate records the environment provisioned for its candidate, under the v2
/// signature.
///
/// <para><b>Read these inputs precisely.</b> They record the environment that was started
/// and attested for the iteration — image identity, engine version, effective resource
/// caps, and the containment the engine reports it actually applied (network mode,
/// read-only rootfs, dropped capabilities, security options — all inside the hashed
/// attestation; the declared write surface rides in the hashed spec). They do <b>not</b>
/// themselves assert that the candidate was compiled or executed
/// inside that environment: these three are provisioning evidence. Compilation containment
/// is a SEPARATE claim carried by the <c>session-build</c> input, minted only when the
/// harness's in-session build leg is enabled and passes (<c>SessionCandidateBuild</c>);
/// execution containment is another, the <c>session-execution</c> input the gate records
/// when the witness, determinism, and mutation legs ran through
/// <c>SessionExecutionBackend</c> (harness execution leg enabled). A certificate carrying
/// only these three inputs was provisioned a session and executed nothing inside it —
/// see the harness's remarks for the boundary.</para>
/// </summary>
public static class SessionEnvironmentInputs
{
    /// <summary>Input kind for the resolved image identity.</summary>
    public const string ImageDigestKind = "image-digest";

    /// <summary>Input kind for the hash of the sandbox spec the session was started from.</summary>
    public const string SandboxSpecKind = "sandbox-spec";

    /// <summary>Input kind for the hash of the environment attestation.</summary>
    public const string AttestationKind = "attestation";

    private static readonly JsonSerializerOptions CanonicalJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Builds the three environment inputs. Fail-closed on identity: an attestation without
    /// a resolved image digest cannot become a certificate input — "some image with this
    /// tag" is not an environment identity.
    /// </summary>
    public static IReadOnlyList<CertificationInput> From(SandboxSpec spec, SessionAttestation attestation)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(attestation);
        if (string.IsNullOrWhiteSpace(attestation.ImageDigest))
        {
            throw new InvalidOperationException(
                $"Session '{attestation.SessionId}' attestation carries no image digest; an unresolved "
                + "image identity cannot be recorded as a certificate input.");
        }

        return new[]
        {
            new CertificationInput
            {
                Kind = ImageDigestKind,
                Id = attestation.Image,
                Hash = attestation.ImageDigest!,
            },
            new CertificationInput
            {
                Kind = SandboxSpecKind,
                Id = attestation.SessionId,
                Hash = BrickContentHasher.ComputeSha256(JsonSerializer.Serialize(spec, CanonicalJson)),
            },
            new CertificationInput
            {
                Kind = AttestationKind,
                Id = attestation.SessionId,
                Hash = BrickContentHasher.ComputeSha256(JsonSerializer.Serialize(attestation, CanonicalJson)),
            },
        };
    }
}
