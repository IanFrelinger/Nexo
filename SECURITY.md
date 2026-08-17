# Security Policy

## Supported versions

Nexo is pre-1.0 and has no tagged release yet (`CHANGELOG.md` is all `[Unreleased]`). Security fixes land on `master`; today `master` is the only supported line. Once releases are tagged, only the most recent release and `master` will be supported.

## Reporting a vulnerability

Please do **not** open a public issue for a security vulnerability.

- Preferred: report privately via [GitHub Security Advisories](https://github.com/IanFrelinger/Nexo/security/advisories/new).
- Alternatively: contact the maintainer, [@IanFrelinger](https://github.com/IanFrelinger).

Include the affected component (project or path), a reproduction or proof of concept, and your assessment of the impact. You should receive an acknowledgement within 7 days.

## Scope

Nexo's security-relevant surface includes the trust path components: policy gates, sanitization and PII/secret filters, audit trails and barrier identity, mesh/federation transport, and execution-target routing (local, cloud, peer). Reports about weaknesses in these boundaries are especially welcome, as are reports about the standard surfaces (`Nexo.API`, `Nexo.CLI`, container images, compose deployments).

## Default posture and in-scope surfaces

What a fresh checkout does when you run it, and what you must turn on before it faces a network.

**What listens where by default**

- `Nexo.API` (`dotnet run`, `.docker/Dockerfile.api`, `docker-compose.portal.yml`) serves HTTP only. `ASPNETCORE_URLS` defaults to loopback for `dotnet run`; every shipped compose file publishes on `127.0.0.1`. `AllowedHosts` is `*` in the shared `appsettings.json` (mesh peers and tailnet names must resolve); `appsettings.Development.json` narrows it to `localhost;127.0.0.1;[::1]`.
- `Nexo:Security:ExposureProfile` is `Localhost` and `Nexo:Security:AuthorizationMode` is `None`: read *and* mutating routes under `/api` answer without credentials on loopback. That is the intended local-development posture, not a deployment posture.
- Nothing under `/api` calls a container runtime or accepts host paths by default: the remote execution surface (below) is unmapped, and the MCP / A2A protocol surfaces are feature-flagged off.

**What needs auth, and how to enable it**

- Set `Nexo:Security:AuthorizationMode` to `ApiKey`, `BearerToken`, `Basic`, or one of the OR-composites, plus the matching credential (`ApiKey`, `BearerToken`, `BasicAuthUsername`/`BasicAuthPassword`). `AuthorizationScope=MutatingApi` (default) credentials POST/PUT/PATCH/DELETE under `/api` and every verb on `/api/mcp` and `/api/a2a`; `AllApi` credentials GETs too. One family of reads is credentialed under either scope: `GET /api/copilot/tasks*` (task history carries the prompts and outputs of past runs), controlled by `Nexo:Security:RequireAuthForCopilotReadApis`, which defaults to `true`. Credentials are compared in constant time against the configured plaintext value (they are not hashed at rest), so keep them in environment or secret stores, not committed JSON. Details: `docs/Configuration.md`, "Nexo.API exposure".
- The legacy `RequireApiKeyForMutatingEndpoints=true` flag fails closed: with no `ApiKey` configured every mutating request is rejected (401) rather than passed through.
- Tenant / org / user identity on the multi-tenant surfaces (`X-Nexo-Tenant`, `X-Nexo-User`, `X-Nexo-Org`) is client-asserted. Trust those headers only behind built-in auth or an authenticating proxy that sets them; `docker-compose.cloud-multi-tenant.yml` therefore requires `NEXO_API_KEY`.

**Exposure fails closed**

- `ExposureProfile` of `Lan`, `Tailnet` or `Public` with `AuthorizationMode=None` (and the legacy flag off) makes `Nexo.API` refuse to start, with a message that names the fix. The profile still does not configure firewalls, Tailscale ACLs or TLS; do that separately.
- Escape hatch: `Nexo:Security:AllowUnauthenticatedNetworkExposure=true` turns the refusal into a startup warning. Set it only when an authenticating reverse proxy or a network ACL is the whole auth story (`scripts/start-nexo-api-dev.*` set it for `--listen-lan` and say so loudly).

**Remote container execution is opt-in**

- `POST /api/execution/build` and `POST /api/execution/run` hand caller-chosen images, commands and host bind mounts to this host's Docker daemon on behalf of a `RemoteExecutionPlatform` client (`NEXO_EXECUTION_REMOTE_URL`). They are mapped only when `Nexo:Execution:ServeRemoteExecution=true` (404 otherwise), refuse `AuthorizationMode=None` with 403 even when opted in, and reject any `VolumeMounts` (400) unless `Nexo:Execution:AllowedVolumeMountRoot` names the single host directory they may live under.
- Shipped compose files never mount `docker.sock` into `Nexo.API`; the in-process `DockerExecutionPlatform` used by `nexo test --platform` on the CLI is unaffected by the opt-in.

**Commercial hosts**

- `Nexo.Commercial.Fleet.Host` and `Nexo.Commercial.GameDirector.Host` run `AuthorizationMode=ApiKey` and ship **no** key: supply `Nexo__Security__ApiKey` (compose: `NEXO_API_KEY`, required with no default) or every mutating request is rejected.
