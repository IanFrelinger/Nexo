namespace Ashlar.API.Security;

/// <summary>HttpContext item keys used by Ashlar authorization middleware.</summary>
public static class AshlarAuthContextKeys
{
    /// <summary>Item key storing the resolved <see cref="AshlarApiAuthTier"/> after authentication.</summary>
    public const string AuthTier = "Ashlar.ApiAuthTier";
}
