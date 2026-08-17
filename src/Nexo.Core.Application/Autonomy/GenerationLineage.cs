using System.Diagnostics.CodeAnalysis;

namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// The recursion pedigree of one candidate (autonomous self-extension spec R4.1):
/// human-authored code is depth 0; an artifact proposed by a loop whose proposer or
/// tooling includes depth-n artifacts is depth n+1. Depth claims are bound to parent
/// certificate identities — the anti-laundering rule (§8): a fresh session cannot reset
/// its depth, because a depth above zero without named parents, or parents without a
/// depth above zero, is refused before anything else runs.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed record GenerationLineage
{
    /// <summary>Recursion depth: 0 = human-authored proposer context; n+1 = produced using depth-n artifacts.</summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Certificate signatures of the self-produced artifacts in the proposer/tooling chain
    /// that make this candidate depth &gt; 0. The certifier hashes these into the record's
    /// inputs, so the claimed depth is verifiable against the lineage it rests on.
    /// </summary>
    public IReadOnlyList<string> ParentCertificateSignatures { get; init; } = Array.Empty<string>();

    /// <summary>Lineage of a human-authored proposer context.</summary>
    public static GenerationLineage HumanAuthored { get; } = new() { Depth = 0 };

    /// <summary>
    /// Derives the lineage of an artifact produced USING an artifact certified with
    /// <paramref name="parentCertificateSignature"/> under <paramref name="parent"/>.
    /// Depth increments; the parent chain accumulates. Capability only narrows with
    /// depth (R4.4) — that check lives with the session assembly, not here.
    /// </summary>
    public static GenerationLineage Child(GenerationLineage parent, string parentCertificateSignature)
    {
        if (parent is null)
            throw new ArgumentNullException(nameof(parent));
        if (string.IsNullOrWhiteSpace(parentCertificateSignature))
            throw new ArgumentException("The parent's certificate signature is required.", nameof(parentCertificateSignature));

        return new GenerationLineage
        {
            Depth = parent.Depth + 1,
            ParentCertificateSignatures = parent.ParentCertificateSignatures
                .Append(parentCertificateSignature)
                .ToArray(),
        };
    }

    /// <summary>
    /// Every way this lineage is inconsistent (the laundering matrix). Empty = coherent.
    /// A coherent lineage can still exceed the ceiling — that is
    /// <see cref="RecursionDiscipline"/>'s check, kept separate so the refusal messages
    /// distinguish "you lied about depth" from "you are too deep".
    /// </summary>
    public IReadOnlyList<string> FindInconsistencies()
    {
        var problems = new List<string>();
        if (Depth < 0)
            problems.Add($"depth {Depth} is negative");
        if (Depth == 0 && ParentCertificateSignatures.Count > 0)
            problems.Add("depth 0 claims parent artifacts — human-authored context has no self-produced parents");
        if (Depth > 0 && ParentCertificateSignatures.Count == 0)
            problems.Add($"depth {Depth} names no parent certificates — an unattributed depth claim is laundering (R4.1)");
        if (ParentCertificateSignatures.Any(string.IsNullOrWhiteSpace))
            problems.Add("parent certificate list contains a blank signature");
        if (Depth > 0 && ParentCertificateSignatures.Count > Depth)
            problems.Add($"depth {Depth} names {ParentCertificateSignatures.Count} parents — more parents than generations");
        return problems;
    }
}

/// <summary>
/// The hard recursion ceiling (R4.2), enforced by the certifier and independently by the
/// swap host. Default 2. Raising it is a Tier 2 change — which is why the environment
/// variable can only LOWER it: a deployment may be stricter than this file, never looser.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class RecursionDiscipline
{
    /// <summary>Environment variable that may LOWER the ceiling (stricter deployments only).</summary>
    public const string CeilingEnvVar = "NEXO_GENERATION_DEPTH_CEILING";

    /// <summary>The spec-default hard ceiling. Changing this constant is a Tier 2 change.</summary>
    public const int DefaultCeiling = 2;

    /// <summary>Resolves the effective ceiling: the default, lowerable by environment, never raisable.</summary>
    public static int ResolveCeiling()
    {
        var raw = Environment.GetEnvironmentVariable(CeilingEnvVar);
        if (int.TryParse(raw, out var configured) && configured >= 0 && configured < DefaultCeiling)
            return configured;
        return DefaultCeiling;
    }

    /// <summary>
    /// Full refusal check for one lineage: consistency (laundering matrix) plus the
    /// ceiling. Empty = admissible depth-wise.
    /// </summary>
    public static IReadOnlyList<string> FindViolations(GenerationLineage lineage)
    {
        if (lineage is null)
            throw new ArgumentNullException(nameof(lineage));

        var problems = new List<string>(lineage.FindInconsistencies());
        var ceiling = ResolveCeiling();
        if (lineage.Depth > ceiling)
        {
            problems.Add(
                $"generation depth {lineage.Depth} exceeds the ceiling {ceiling} (R4.2; raising the ceiling is Tier 2)");
        }

        return problems;
    }
}
