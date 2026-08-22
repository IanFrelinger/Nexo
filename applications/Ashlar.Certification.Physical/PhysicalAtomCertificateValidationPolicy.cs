using System.Text.RegularExpressions;

namespace Ashlar.Certification.Physical;

/// <summary>
/// Binding-scope validation policy for physical-atom certificates.
/// </summary>
public static class PhysicalAtomCertificateValidationPolicy
{
    private static readonly Regex SemVerPattern = new(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AssetHashPattern = new(
        "^[0-9a-f]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool ValidateBindingScopeConsistency(
        PhysicalAtomCertificate certificate,
        out string? failureCode,
        out string? reason)
    {
        if (certificate.AtomId == Guid.Empty)
        {
            failureCode = "atom-id-invalid";
            reason = "atom_id must be a non-empty UUID.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(certificate.AssetHash) || !AssetHashPattern.IsMatch(certificate.AssetHash))
        {
            failureCode = "asset-hash-invalid";
            reason = "asset_hash must be a 64-character lowercase SHA-256 hex digest.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(certificate.AssetVersion) || !SemVerPattern.IsMatch(certificate.AssetVersion))
        {
            failureCode = "asset-version-invalid";
            reason = "asset_version must be a valid SemVer string.";
            return false;
        }

        if (certificate.SchemaVersion != PhysicalAtomCertificate.CurrentSchemaVersion)
        {
            failureCode = "schema-version-unsupported";
            reason = $"schema_version {certificate.SchemaVersion} is not supported (expected {PhysicalAtomCertificate.CurrentSchemaVersion}).";
            return false;
        }

        switch (certificate.BindingScope)
        {
            case BindingScope.Design:
                if (certificate.ManufactureMeta is not null)
                {
                    failureCode = "binding-scope-manufacture-meta-forbidden";
                    reason = "Design binding_scope forbids manufacture_meta; populated manufacture_meta must be rejected.";
                    return false;
                }

                break;

            case BindingScope.Instance:
                if (certificate.ManufactureMeta is null)
                {
                    failureCode = "binding-scope-manufacture-meta-required";
                    reason = "Instance binding_scope requires non-null manufacture_meta.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(certificate.ManufactureMeta.SerialNumber))
                {
                    failureCode = "binding-scope-serial-required";
                    reason = "Instance binding_scope requires manufacture_meta.serial_number.";
                    return false;
                }

                break;

            case BindingScope.Batch:
                if (certificate.ManufactureMeta is null)
                {
                    failureCode = "binding-scope-manufacture-meta-required";
                    reason = "Batch binding_scope requires non-null manufacture_meta.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(certificate.ManufactureMeta.BatchId))
                {
                    failureCode = "binding-scope-batch-required";
                    reason = "Batch binding_scope requires manufacture_meta.batch_id.";
                    return false;
                }

                break;

            default:
                failureCode = "binding-scope-unknown";
                reason = $"Unknown binding_scope '{certificate.BindingScope}'.";
                return false;
        }

        if (certificate.GeoAnchor is not null && !GeoAnchorValidator.IsConsistent(certificate.GeoAnchor, out var geoReason))
        {
            failureCode = "geo-anchor-inconsistent";
            reason = geoReason;
            return false;
        }

        failureCode = null;
        reason = null;
        return true;
    }
}
