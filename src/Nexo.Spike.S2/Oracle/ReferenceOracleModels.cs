namespace Nexo.Spike.S2.Oracle;

public sealed record ReferenceLabel(
    IReadOnlyList<string> Inputs,
    string ExpectedType,
    string Source);

public sealed record ReferenceOracle(
    string Version,
    string Provenance,
    IReadOnlyList<ReferenceLabel> Labels)
{
    public const string VersionId = "s2.0-v1";

    public IReadOnlyList<ReferenceLabel> HeldOutLabels =>
        Labels.Where(l => string.Equals(l.Source, "held-out", StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<ReferenceLabel> GatePinnedLabels =>
        Labels.Where(l => string.Equals(l.Source, "gate-pinned", StringComparison.OrdinalIgnoreCase)).ToList();
}

public sealed record OracleJudgment(
    bool Agrees,
    IReadOnlyList<OracleDisagreement> Disagreements);

public sealed record OracleDisagreement(
    IReadOnlyList<string> Inputs,
    string ExpectedType,
    string ActualType);
