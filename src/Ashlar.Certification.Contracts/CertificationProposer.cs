namespace Ashlar.Certification.Contracts;

/// <summary>
/// Identity and parameters of the proposer whose artifact was certified
/// (trust-loop spec §2.1 <c>proposer</c>).
/// </summary>
public sealed record CertificationProposer
{
    /// <summary>Proposer identity (agent id, model id, or <c>human</c>).</summary>
    public required string Identity { get; init; }

    /// <summary>Proposal-relevant parameters; keys are ordinal-sorted in the signed payload.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Recorded RNG seed when the proposal pipeline used randomness.</summary>
    public string? Seed { get; init; }
}
