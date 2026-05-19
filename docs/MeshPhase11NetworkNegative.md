# Phase 11 — Network negative verification

Phase 11 hardens the virtual mesh lab against **network failure modes** that happy-path verify scripts do not cover. The director control plane (peer-a) is tested separately from **worker reachability** on the Docker bridge.

**Depends on:** peer-a + peer-b running, Phase 9 LiteDB on peer-a (for director-restart lease test).

## Scenarios ([`mesh-lab-verify-network-negative.sh`](../scripts/mesh-lab-verify-network-negative.sh))

| # | Scenario | Expect |
|---|----------|--------|
| 1 | Fleet peer with **blackhole** `apiBaseUrl` (`10.255.255.254`) | Schedule → **Assigned**; bridge `curl` to worker **fails** (no preflight) |
| 2 | Fleet peer with **unresolvable DNS** (`.invalid` host) | Schedule → **Assigned**; bridge `curl` **fails** |
| 3 | **Drained-only** fleet | Schedule → **400**; `placementReason` indicates no eligible nodes |
| 4 | **peer-b stopped** while task **Running** | Bridge cannot reach `peer-b:8080`; after `start`, health OK; director **PATCH** still works |
| 5 | **peer-a restart** (LiteDB) | Task + **leaseToken** survive; **PATCH Running** succeeds |

Shared helpers: [`scripts/mesh-lab-net.sh`](../scripts/mesh-lab-net.sh).

## Run

```bash
make mesh-lab-e2e-workers          # includes network-negative after trust
make mesh-lab-verify-network-negative   # lab already up
```

Skip: `MESH_LAB_SKIP_NETWORK_NEGATIVE_VERIFY=1`

## What this does not replace

- **TLS / Tailscale / two physical hosts** — see [`docs/runbooks/mesh-lab-operations.md`](runbooks/mesh-lab-operations.md) manual checklist.
- **gRPC capability transport** — `dotnet test src/Nexo.Tests.Transport`.

## Revision history

| Date | Change |
|------|--------|
| 2026-05-19 | Initial network-negative verify script + CI wiring. |
