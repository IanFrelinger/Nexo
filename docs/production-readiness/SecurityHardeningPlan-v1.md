# Security & trust hardening plan v1

Validates the **trust boundary** (policy packs, audit log, access boundary), **API auth & mesh security middleware**, **CLI trust surfaces**, **supply chain**, and **air-gapped operation** — the production "waterproofing" layer before exposing Ashlar to the internet.

**Automation:** `make security-gate-full`

## Prerequisites

```bash
make ship-gate-full
```

## Tiers

| Tier | Focus | Command |
|------|--------|---------|
| A | Trust core (policy packs, peer trust, audit, access boundary) | `make security-gate-tier-a` |
| B | API security middleware on **net10.0** (key/bearer/basic, mesh, open-internet readiness; counted, fail-closed on zero tests) | `make security-gate-tier-b` |
| C | Trust CLI (`trust boundary`, `trust dashboard`, `trust audit`) | `make security-gate-tier-c` |
| D | Supply chain (vulnerable + deprecated packages) | `make security-gate-tier-d` |
| E | Air-gapped + safety probes | `make security-gate-tier-e` |

## Flags

| Variable | Effect |
|----------|--------|
| `SECURITY_GATE_SKIP_PRIOR=1` | Skip ship-gate prerequisite |
| `SECURITY_GATE_SKIP_TIER_D=1` | Skip supply-chain scan |
| `SECURITY_GATE_SKIP_TIER_E=1` | Skip air-gapped tier |
| `SECURITY_GATE_STRICT_SUPPLY_CHAIN=1` | Tier D fails on any vulnerable transitive |
| `SECURITY_GATE_AIRGAPPED_CONTAINER=1` | Tier E requires a working Docker daemon and runs the `--network none` container suite; missing Docker is a failure, not a skip |

## What each tier proves

**A** — `TrustPolicyPackRegistryTests`, `AshlarPeerBrickExecutorTrustTests`, audit log retention, `AccessBoundary` rules.

**B** — `AshlarApiKeyAuthMiddlewareTests`, `MeshSecurityMiddlewareTests`, `AshlarApiOpenInternetReadinessTests`, `SecurityAdvisoryEndpointTests`, `SecurityAnalysisRuleTests`.

**C** — TrustCommand unit suite + `ashlar trust boundary --format-json` and `ashlar trust dashboard --format-json` smoke.

**D** — `dotnet list package --vulnerable` / `--deprecated` on `application/Ashlar.Application.sln` plus `Ashlar.Hosting` and `Ashlar.Infrastructure` (avoids a known NuGet client issue with YamlDotNet registration on `Ashlar.Core`). Reports in `.ashlar/security-gate/`.

**E** — air-gapped profile resolution + `LocalModelProviderSafetyTests`. Optional `--network none` container suite.

## Related

- [Security & trust](SecurityAndTrust.md)
- [Trust & execution boundaries](../architecture/TrustAndExecutionBoundaries.md)
- `.github/workflows/test-air-gapped-no-network.yml`
