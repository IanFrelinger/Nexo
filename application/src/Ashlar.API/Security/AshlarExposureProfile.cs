namespace Ashlar.API.Security;

/// <summary>
/// Declares how Ashlar.API is expected to be reached. User-configurable; drives advisory text only (no enforcement).
/// </summary>
public enum AshlarExposureProfile
{
    /// <summary>Loopback / SSH tunnel only.</summary>
    Localhost,

    /// <summary>Trusted LAN / Wi‑Fi; e.g. -ListenLan on home network.</summary>
    Lan,

    /// <summary>Tailscale (or similar) private overlay; enforce with ACLs in the tailnet admin.</summary>
    Tailnet,

    /// <summary>Internet-routable; requires TLS + authentication in front per operations docs.</summary>
    Public
}
