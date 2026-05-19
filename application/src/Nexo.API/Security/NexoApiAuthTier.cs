namespace Nexo.API.Security;

/// <summary>
/// Credential tier inferred after built-in Nexo.API authorization succeeds.
/// </summary>
public enum NexoApiAuthTier
{
    None = 0,
    Full = 1,
    CopilotScoped = 2
}

public static class NexoAuthContextKeys
{
    public const string AuthTier = "Nexo.ApiAuthTier";
}
