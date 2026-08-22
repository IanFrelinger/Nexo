namespace Ashlar.API.Security;

/// <summary>
/// Credential tier inferred after built-in Ashlar.API authorization succeeds.
/// </summary>
public enum AshlarApiAuthTier
{
    /// <summary>No authenticated tier (anonymous or bypassed routes).</summary>
    None = 0,

    /// <summary>Full API access tier.</summary>
    Full = 1,

    /// <summary>Copilot-scoped access tier with reduced mutating privileges.</summary>
    CopilotScoped = 2
}
