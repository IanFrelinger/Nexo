using System.Text;

namespace Nexo.Core.Application.Generation;

/// <summary>
/// Provenance tier of a context element (trust-loop spec R1.5). Lower is more
/// trustworthy; assembly orders machine-readable ground truth ahead of documentation,
/// and documentation ahead of model recall.
/// </summary>
public enum ProposalContextTier
{
    /// <summary>Machine-readable ground truth (bindings, schemas, grammars, reference corpora).</summary>
    MachineReadable = 0,

    /// <summary>Human-written documentation.</summary>
    Documentation = 1,

    /// <summary>Model-recalled or otherwise unverified content.</summary>
    ModelRecall = 2
}

/// <summary>One candidate context element offered to the assembler.</summary>
public sealed record ProposalContextSource
{
    /// <summary>Source class the per-class char budget applies to (e.g. <c>witness</c>, <c>reference</c>).</summary>
    public required string Kind { get; init; }

    /// <summary>Identifier within the source class.</summary>
    public required string Id { get; init; }

    /// <summary>Raw content.</summary>
    public required string Content { get; init; }

    /// <summary>Provenance tier (spec R1.5).</summary>
    public ProposalContextTier Tier { get; init; } = ProposalContextTier.Documentation;
}

/// <summary>Explicit per-source-class char budget (spec R1.4).</summary>
public sealed record ProposalContextBudget
{
    /// <summary>Source class the cap applies to.</summary>
    public required string Kind { get; init; }

    /// <summary>Maximum total characters this class may contribute.</summary>
    public required int MaxChars { get; init; }
}

/// <summary>One element included in the assembled context, with its truncation record.</summary>
public sealed record ProposalContextElement
{
    /// <summary>Source class.</summary>
    public required string Kind { get; init; }

    /// <summary>Source identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Provenance tier.</summary>
    public required ProposalContextTier Tier { get; init; }

    /// <summary>Included (possibly truncated) canonicalized content.</summary>
    public required string Content { get; init; }

    /// <summary>Whether the content was cut to fit the class budget.</summary>
    public required bool Truncated { get; init; }

    /// <summary>Canonicalized length before truncation.</summary>
    public required int OriginalLength { get; init; }

    /// <summary>Base64 SHA-256 of the included content.</summary>
    public required string ContentHash { get; init; }
}

/// <summary>
/// Deterministically assembled stage-1 proposal context (trust-loop spec §1). Same
/// sources + budgets + seed always produce the same elements, rendering, and
/// <see cref="Hash"/> — which is what the certificate's <c>inputs</c> records.
/// </summary>
public sealed record ProposalContext
{
    /// <summary>Base64 SHA-256 over the canonical description of the whole context.</summary>
    public required string Hash { get; init; }

    /// <summary>Included elements in canonical order (tier, then kind, then id).</summary>
    public required IReadOnlyList<ProposalContextElement> Elements { get; init; }

    /// <summary>Ids (<c>kind/id</c>) of sources excluded entirely because their class budget was spent (spec R1.4).</summary>
    public required IReadOnlyList<string> OmittedSourceIds { get; init; }

    /// <summary>Recorded seed for any randomized preparation step (spec R1.2); null when none was used.</summary>
    public string? Seed { get; init; }

    /// <summary>
    /// Deterministic text rendering for prompts/grounding: one titled section per
    /// element, in canonical order.
    /// </summary>
    public string RenderText()
    {
        var builder = new StringBuilder();
        foreach (var element in Elements)
        {
            builder.Append("== ").Append(element.Kind).Append('/').Append(element.Id)
                .Append(" (").Append(element.Tier.ToString().ToLowerInvariant());
            if (element.Truncated)
                builder.Append(", truncated");
            builder.AppendLine(") ==");
            builder.AppendLine(element.Content);
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }
}
