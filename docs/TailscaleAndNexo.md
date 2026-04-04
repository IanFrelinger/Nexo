# Tailscale with Nexo

Tailscale is a **mesh VPN** (WireGuard-based). It does not run *inside* Nexo; you install it on the **host** (or use a container sidecar). Nexo integrates **configuration and guidance** so you can declare an **exposure profile** and see **advisory** text in the Director portal.

## What Nexo does vs what Tailscale does

| Layer | Responsibility |
|--------|----------------|
| **Tailscale** | Private connectivity (`100.x` addresses), **ACLs** for who can reach which ports on which nodes. |
| **Nexo (`Nexo:Security`)** | User-set **ExposureProfile** (`Localhost`, `Lan`, `Tailnet`, `Public`), optional **CustomAdvisory**, portal banner. **Advisory only** — not a substitute for ACLs or TLS. |

## Recommended layout (personal / small team)

1. Install **Tailscale** on the machine that runs **Nexo.API** (Windows, macOS, Linux).
2. Install Tailscale on **phone/laptop** you use to access the portal.
3. Keep Nexo bound to **`http://0.0.0.0:8080`** *or* `127.0.0.1:8080` depending on preference:
   - **`0.0.0.0`**: reachable on all interfaces; **restrict who can connect** with host firewall **and** Tailscale ACLs.
   - **`127.0.0.1`**: only local; use **SSH / another hop** — usually unnecessary if Tailscale reaches the node and you bind appropriately.

4. Set Nexo’s profile so the portal and logs match your intent:

```bash
export Nexo__Security__ExposureProfile=Tailnet
# optional:
export Nexo__Security__CustomAdvisory="Team: use tag:nexo only"
export Nexo__Security__ShowAdvisoryInPortal=true
```

See **`docs/config/security-exposure.env.example`** for all keys.

5. In the **Tailscale admin console**, define **ACLs** so only the right identities (tags, users, groups) can reach **TCP 8080** on the Nexo node. Example pattern (adapt names):

```json
{
  "groups": { "group:nexo": ["you@github", "partner@github"] },
  "tagOwners": { "tag:nexo": ["autogroup:admin"] },
  "acls": [
    { "action": "accept", "src": ["group:nexo"], "dst": ["tag:nexo:8080"] }
  ],
  "nodeAttrs": [
    { "target": ["tag:nexo"], "attr": ["funnel"] }
  ]
}
```

Use your real `src`/`dst` rules; the above is illustrative — **do not copy blindly** without matching your tailnet.

6. **Do not** expose **Ollama (11434)** to the wide tailnet unless you intend to; keep Ollama on `127.0.0.1` on the host and let only Nexo talk to it.

## API: advisory endpoint

`GET /api/security/advisory` returns JSON for operators and the portal:

- `exposureProfile`, `summary`, `hints[]`, `customAdvisory`, `showInPortal`

No secrets are included.

## Docker Compose

Official **Tailscale in Docker** patterns exist (sidecar `tailscaled`, `userspace-networking`, etc.). If Nexo runs in Compose on a server:

- Either run **Tailscale on the host** and publish Nexo only to localhost + ACLs, **or**
- Follow [Tailscale’s Docker documentation](https://tailscale.com/kb/1282/docker) to add a sidecar and route only through the tailnet.

Nexo’s Dockerfiles do **not** embed `tailscale`; keep upgrades and keys in your infra layer.

## When exposure is `Public`

If you set `Nexo__Security__ExposureProfile=Public`, Nexo logs a **warning** at startup. You must still place **TLS + authentication** in front of the API for real Internet exposure — see `docs/SelfHostedGameServerPortal.md`.

## Related

- `docs/config/security-exposure.env.example`
- `docs/SelfHostedGameServerPortal.md` — checklists and Internet patterns
- `scripts/start-nexo-api-dev.ps1` / `.sh` — sets `Localhost` or `Lan` automatically unless you override
