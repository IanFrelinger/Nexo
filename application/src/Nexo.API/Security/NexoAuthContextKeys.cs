namespace Nexo.API.Security;

/// <summary>HttpContext item keys used by Nexo authorization middleware.</summary>
public static class NexoAuthContextKeys
{
    /// <summary>Item key storing the resolved <see cref="NexoApiAuthTier"/> after authentication.</summary>
    public const string AuthTier = "Nexo.ApiAuthTier";
}
