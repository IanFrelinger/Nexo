namespace Ashlar.API.Security;

/// <summary>Security advisory content.</summary>
internal static class SecurityAdvisoryContent
{
    /// <summary>Returns exposure-specific advisory summary and operator hints.</summary>
    public static (string Summary, string[] Hints) For(AshlarExposureProfile profile)
    {
        return profile switch
        {
            AshlarExposureProfile.Localhost => (
                "Exposure profile: localhost — API intended for this machine only.",
                [
                    "Ashlar.API has no built-in login; loopback binding limits who can reach it.",
                    "For remote use, prefer Tailscale or SSH port forwarding instead of opening router ports."
                ]),
            AshlarExposureProfile.Lan => (
                "Exposure profile: LAN — reachable from other devices on your local network.",
                [
                    "Restrict with host firewall rules to your subnet if possible.",
                    "Do not use LAN binding on untrusted Wi‑Fi; anyone on that network may reach the API.",
                    "See docs/SelfHostedAgentServer.md for the security checklist."
                ]),
            AshlarExposureProfile.Tailnet => (
                "Exposure profile: tailnet — reachable only over your Tailscale (or similar) private network.",
                [
                    "Configure Tailscale ACLs so only intended tags/users can reach this host and port.",
                    "Keep Ashlar bound to a non-public interface where possible; rely on ACLs for who can connect.",
                    "See docs/TailscaleAndAshlar.md for layout options and examples."
                ]),
            AshlarExposureProfile.Public => (
                "Exposure profile: public — Internet or untrusted path to the API.",
                [
                    "You must terminate TLS and enforce authentication in front of Ashlar (reverse proxy, Cloudflare Access, etc.).",
                    "Do not expose Ollama (11434) or Docker sockets publicly.",
                    "See docs/SelfHostedAgentServer.md → Exposing Ashlar on the public Internet."
                ]),
            _ => For(AshlarExposureProfile.Localhost)
        };
    }
}
