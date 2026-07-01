# Physical-Atom Phase 1 — Test Report

**Sprint:** Phase 1 — Asset Resolution + Certified Bundles  
**Maturity:** Prototype  
**Methodology:** Rejection-first; resolution witness independent of pipeline tests.

## Rejection coverage (`PhysicalAtomResolutionVerifierTests`)

| ID | Case | Expected failure | Status |
|----|------|------------------|--------|
| R1 | Unregistered `atom_id` | `atom-unresolved` | PASS |
| R2 | Store returns bytes that don't match cert hash | `asset-hash-mismatch` | PASS |
| R3 | Registered cert but missing asset | `asset-unresolved` | PASS |
| R4 | Tampered bundle cert hash vs embedded asset | `bundle-asset-hash-mismatch` | PASS |

## Happy-path coverage

| ID | Case | Status |
|----|------|--------|
| A1 | Resolution store + cert verifies | PASS |
| A2 | Self-contained bundle verifies | PASS |
| A3 | Pipeline certify-and-register + resolution verify | PASS |
| A4 | Sample bundle manifest round-trip | PASS |

## Pipeline coverage (`AssetBundleCertificationPipelineTests`)

| Case | Status |
|------|--------|
| Certify, register, resolve, verify end-to-end | PASS |
| Refuse invalid binding-scope at pipeline | PASS |

## Execution

```bash
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 \
  --filter "FullyQualifiedName~PhysicalAtomResolution|FullyQualifiedName~AssetBundleCertification|FullyQualifiedName~PhysicalAtomCertBundleManifest"
```

All tests headless (xUnit, in-memory store, no network).

## Phase 1 gate

Resolution verifier rejects unresolved/tampered hosting paths; accepts well-formed bundles and pipeline output. **9/9 Phase 1 tests passing** (included in cert-gate total).
