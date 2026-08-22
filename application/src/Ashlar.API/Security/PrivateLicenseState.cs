namespace Ashlar.API.Security;

/// <summary>Enumerates private license state values.</summary>
public enum PrivateLicenseState
{
    /// <summary>No license file is configured.</summary>
    NotConfigured,

    /// <summary>License is valid and not expired.</summary>
    Valid,

    /// <summary>License has expired.</summary>
    Expired,

    /// <summary>License file is missing, malformed, or failed verification.</summary>
    Invalid,
}
