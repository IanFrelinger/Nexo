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
/// caps. As of today they do <b>not</b> assert that the candidate was compiled or executed
/// inside that environment: <c>AutonomousIterationHarness</c> certifies in-process and
/// never calls <c>ISandboxedSession.ExecAsync</c>. Provisioning evidence, not a
/// containment guarantee — see the harness's remarks for the tracked change that closes
/// the difference.</para>
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
