using System.Diagnostics.CodeAnalysis;

namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// Tracks rollback evidence per objective lineage (autonomous self-extension spec R5.5):
/// repeated rollback on one lineage demotes it to Tier 1. The asymmetry is deliberate
/// (I-2): autonomy is LOST on evidence here, but it is never GAINED on evidence anywhere —
/// there is intentionally no "promote" operation on this interface.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public interface ILineageAuthority
{
    /// <summary>Records one rollback against a lineage and returns its total rollback count.</summary>
    int RecordRollback(string lineageKey);

    /// <summary>Whether the lineage has lost Tier-0 autonomy.</summary>
    bool IsDemoted(string lineageKey);

    /// <summary>
    /// Demotes a lineage outright (R7.2: a human may demote any lineage at any time as a
    /// single operation, no proposal session required). There is no promote counterpart.
    /// </summary>
    void Demote(string lineageKey, string reason);
}

/// <summary>Shared default for how many rollbacks demote a lineage (spec default 2).</summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class LineageDemotion
{
    /// <summary>Rollback count at which a lineage loses Tier-0 autonomy.</summary>
    public const int DefaultThreshold = 2;
}
