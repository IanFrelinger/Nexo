namespace Nexo.API.Security;

internal static class SecurityAdvisoryContent
{
    public static (string Summary, string[] Hints) For(NexoExposureProfile profile)
    {
        return profile switch
        {
            NexoExposureProfile.Localhost => (
                "Exposure profile: localhost — API intended for this machine only.",
                [
                    "Nexo.API has no built-in login; loopback binding limits who can reach it.",
                    "For remote use, prefer Tailscale or SSH port forwarding instead of opening router ports."
                ]),
            NexoExposureProfile.Lan => (
                "Exposure profile: LAN — reachable from other devices on your local network.",
                [
                    "Restrict with host firewall rules to your subnet if possible.",
                    "Do not use LAN binding on untrusted Wi‑Fi; anyone on that network may reach the API.",
                    "See docs/SelfHostedGameServerPortal.md for the security checklist."
                ]),
            NexoExposureProfile.Tailnet => (
                "Exposure profile: tailnet — reachable only over your Tailscale (or similar) private network.",
                [
                    "Configure Tailscale ACLs so only intended tags/users can reach this host and port.",
                    "Keep Nexo bound to a non-public interface where possible; rely on ACLs for who can connect.",
                    "See docs/TailscaleAndNexo.md for layout options and examples."
                ]),
            NexoExposureProfile.Public => (
                "Exposure profile: public — Internet or untrusted path to the API.",
                [
                    "You must terminate TLS and enforce authentication in front of Nexo (reverse proxy, Cloudflare Access, etc.).",
                    "Do not expose Ollama (11434) or Docker sockets publicly.",
                    "See docs/SelfHostedGameServerPortal.md → Exposing Nexo on the public Internet."
                ]),
            _ => For(NexoExposureProfile.Localhost)
        };
    }
}
