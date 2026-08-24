namespace Ashlar.Manifest.Admission;

/// <summary>
/// A change the running application wants to make to itself, after the gate has evaluated
/// its courses. The admission layer receives proposals whose courses have already run —
/// evaluation is the gate's job; deciding what the OUTCOME means under the policy's mode is
/// this layer's job.
/// </summary>
public sealed record ExtensionProposal
{
    /// <summary>Stable proposal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>What kind of thing this adds. Checked against <c>selfExtend.mayAdd</c> —
    /// only kinds inside the envelope are admissible at all.</summary>
    public required string Kind { get; init; }

    /// <summary>One-line human summary, e.g. "add brick invoice.classify.v2".</summary>
    public required string Summary { get; init; }

    /// <summary>Which agent proposed it.</summary>
    public required string ProposedBy { get; init; }

    /// <summary>When it was proposed (UTC).</summary>
    public required DateTimeOffset ProposedAt { get; init; }

    /// <summary>The course results the gate produced for this proposal.</summary>
    public required IReadOnlyList<CourseResult> Courses { get; init; }

    /// <summary>Compact diff or change description, for the review surface.</summary>
    public string Diff { get; init; } = string.Empty;

    /// <summary>
    /// Forge proposal ids holding this extension's actual file changes (M1 propose → hold →
    /// apply). Empty when the extension carries no mediated writes. Seating the stone
    /// applies these; refusing rejects them.
    /// </summary>
    public IReadOnlyList<string> ForgeProposalIds { get; init; } = [];
}

/// <summary>Terminal and intermediate states of a proposal. See SPEC-004: transition
/// authority is the security model.</summary>
public enum ProposalState
{
    /// <summary>All courses passed; awaiting a human verdict (proposing mode, or budget
    /// exhaustion in self-extending mode).</summary>
    Held,

    /// <summary>Admitted — either automatically (self-extending, within budget) or by a
    /// human deciding a held proposal.</summary>
    Admitted,

    /// <summary>Refused by a human, with a recorded reason that feeds back to the proposer.</summary>
    Refused,

    /// <summary>Refused automatically: a course failed, the kind is outside the envelope,
    /// or the mode is sealed. No human involved; the reason names the rule.</summary>
    Rejected,
}

/// <summary>The admission layer's verdict on an evaluated proposal.</summary>
public sealed record AdmissionOutcome
{
    /// <summary>Resulting state.</summary>
    public required ProposalState State { get; init; }

    /// <summary>Why — always populated, because a refusal that does not teach produces the
    /// same proposal again tomorrow.</summary>
    public required string Reason { get; init; }
}
