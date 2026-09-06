using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>Verifies all constituent bricks in a composition are certified and admitted.</summary>
internal static class CompositionConstituentChecker
{
    /// <summary>Check.</summary>
    public static ConstituentCheckResult Check(
        CompositionSpec spec,
        ICertificationRecordStore brickCertificationStore,
        CertificationRecordSigner signer)
    {
        var violations = new List<string>();

        foreach (var node in spec.Nodes)
        {
            var record = brickCertificationStore.Get(node.BrickId);
            if (record is null)
            {
                violations.Add($"Constituent brick '{node.BrickId}' has no certification record");
                continue;
            }

            if (!record.Admitted || !record.Signed || !string.Equals(record.Status, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"Constituent brick '{node.BrickId}' is not admitted (status={record.Status})");
                continue;
            }

            if (!signer.Verify(record, CertificationVerifyOptions.Strict))
            {
                violations.Add($"Constituent brick '{node.BrickId}' has invalid certification signature");
            }
        }

        return new ConstituentCheckResult(violations.Count == 0, violations);
    }

    internal sealed record ConstituentCheckResult(bool Passed, IReadOnlyList<string> Violations);
}
