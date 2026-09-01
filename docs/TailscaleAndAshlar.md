# Tailscale with Ashlar

Tailscale is a **mesh VPN** (WireGuard-based). It does not run *inside* Ashlar; you install it on the **host** (or use a container sidecar). Ashlar integrates **configuration and guidance** so you can declare an **exposure profile** and see **advisory** text in the Director portal.

## What Ashlar does vs what Tailscale does

| Layer | Responsibility |
|--------|----------------|
| **Tailscale** | Private connectivity (`100.x` addresses), **ACLs** for who can reach which ports on which nodes. |
| **Ashlar (`Ashlar:Security`)** | User-set **ExposureProfile** (`Localhost`, `Lan`, `Tailnet`, `Public`), optional **CustomAdvisory**, portal banner. Not a substitute for ACLs or TLS — but off-loopback profiles **refuse to start** without built-in auth unless `AllowUnauthenticatedNetworkExposure=true` (see `SECURITY.md`). |

## Recommended layout (personal / small team)

1. Install **Tailscale** on the machine that runs **Ashlar.API** (Windows, macOS, Linux).
2. Install Tailscale on **phone/laptop** you use to access the portal.
3. Keep Ashlar bound to **`http://0.0.0.0:8080`** *or* `127.0.0.1:8080` depending on preference:
   - **`0.0.0.0`**: reachable on all interfaces; **restrict who can connect** with host firewall **and** Tailscale ACLs.
   - **`127.0.0.1`**: only local; use **SSH / another hop** — usually unnecessary if Tailscale reaches the node and you bind appropriately.

4. Set Ashlar’s profile so the portal and logs match your intent, **plus** a built-in auth mode — `Tailnet` (like `Lan` and `Public`) refuses to start with `AuthorizationMode=None`:

```bash
export Ashlar__Security__ExposureProfile=Tailnet
export Ashlar__Security__AuthorizationMode=ApiKey
export Ashlar__Security__ApiKey="$(openssl rand -hex 32)"
# optional:
export Ashlar__Security__CustomAdvisory="Team: use tag:ashlar only"
export Ashlar__Security__ShowAdvisoryInPortal=true
# only if Tailscale ACLs / an authenticating proxy are your whole auth story:
# export Ashlar__Security__AllowUnauthenticatedNetworkExposure=true
```

See **`docs/config/security-exposure.env.example`** for all keys.

5. In the **Tailscale admin console**, define **ACLs** so only the right identities (tags, users, groups) can reach **TCP 8080** on the Ashlar node. Example pattern (adapt names):

```json
{
  "groups": { "group:ashlar": ["you@github", "partner@github"] },
  "tagOwners": { "tag:ashlar": ["autogroup:admin"] },
  "acls": [
    { "action": "accept", "src": ["group:ashlar"], "dst": ["tag:ashlar:8080"] }
  ],
  "nodeAttrs": [
    { "target": ["tag:ashlar"], "attr": ["funnel"] }
  ]
}
```

Use your real `src`/`dst` rules; the above is illustrative — **do not copy blindly** without matching your tailnet.

6. **Do not** expose **Ollama (11434)** to the wide tailnet unless you intend to; keep Ollama on `127.0.0.1` on the host and let only Ashlar talk to it.

## API: advisory endpoint

`GET /api/security/advisory` returns JSON for operators and the portal:

- `exposureProfile`, `summary`, `hints[]`, `customAdvisory`, `showInPortal`

No secrets are included.

## Docker Compose

Official **Tailscale in Docker** patterns exist (sidecar `tailscaled`, `userspace-networking`, etc.). If Ashlar runs in Compose on a server:

- Either run **Tailscale on the host** and publish Ashlar only to localhost + ACLs, **or**
- Follow [Tailscale’s Docker documentation](https://tailscale.com/kb/1282/docker) to add a sidecar and route only through the tailnet.

Ashlar’s Dockerfiles do **not** embed `tailscale`; keep upgrades and keys in your infra layer.

## When exposure is `Public`


## Related

- `docs/config/security-exposure.env.example`
- `scripts/start-ashlar-api-dev.ps1` / `.sh` — sets `Localhost` or `Lan` automatically unless you override
