using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S3.Models;

/// <summary>
/// Promotion-contract fields from <c>docs/spike/promotion-contract.md</c>, plus an offline signature.
/// </summary>
public sealed record SignedCertificationRecord(
    string RecordId,
    RedFailureReason RedReason,
    double MutationScore,
    double MutationThreshold,
    bool MutationSkipped,
    double IntentDensity,
    double EscapeRate,
    bool RoleSeparationAttested,
    string CatalogVersion,
    string ProbeCorpusVersion,
    DateTimeOffset CertifiedAt,
    string Signature);
