using System.Text;
using System.Text.Json.Serialization;

namespace Ashlar.BackgroundAgents.Forge;

/// <summary>
/// A pending file-modification proposal raised by an agent in lieu of writing
/// to the source tree directly. Forge-mediated writes are how we keep the
/// daemon honest in low-trust modes: instead of mutating <c>src/</c> in place,
/// the agent records its intended change and an operator (or, in higher-trust
/// modes, an automated approver) decides whether to apply it.
///
/// <para>Storage layout mirrors <see cref="Ashlar.BackgroundAgents.Objectives.ObjectiveDocument"/>:
/// each proposal lives as a JSON file under
/// <c>.ashlar/runtime-studio/forge/{proposed|approved|rejected|applied|stale}/{id}.json</c>,
/// so the physical layout matches the lifecycle and operators can sift through
/// the queue with plain <c>ls</c>.</para>
/// </summary>
public sealed record ChangeProposal
{
    public required string Id { get; init; }
    public required string TargetPath { get; init; }
    public required string NewContent { get; init; }
    public required string Summary { get; init; }
    public string? AgentId { get; init; }
    public string? ObjectiveId { get; init; }
    public string? Reason { get; init; }
    public ChangeProposalStatus Status { get; init; } = ChangeProposalStatus.Proposed;
    public string? ResolvedBy { get; init; }
    public string? ResolutionNote { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Hash of the file's content at the moment the proposal was raised, used
    /// to detect drift (someone else modified the file before the proposal
    /// could be applied). Null if the proposal is for a brand-new file.
    /// </summary>
    public string? BaseSha256 { get; init; }
}
