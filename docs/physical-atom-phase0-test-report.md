# Physical-Atom Phase 0 — Test Report

**Sprint:** Phase 0 — Cert + Verifier Core  
**Maturity:** Prototype  
**Methodology:** Rejection-first; verifier witness fixtures independent of issuance code path.

## Rejection coverage (verifier witness — `PhysicalAtomCertificateVerifierTests`)

| ID | Case | Expected failure | Status |
|----|------|------------------|--------|
| R1 | Forged / invalid signature | `signature-invalid` | PASS |
| R2 | `asset_hash` mismatch vs provided asset | `asset-hash-mismatch` | PASS |
| R3 | `Instance` scope, null `manufacture_meta` | `binding-scope-manufacture-meta-required` | PASS |
| R4 | `Design` scope with populated `manufacture_meta` | `binding-scope-manufacture-meta-forbidden` | PASS |
| R5 | `geo_anchor` present, `h3_index` inconsistent with lat/lon | `geo-anchor-inconsistent` | PASS |
| R6 | Tampered `extensions` map (signature no longer valid) | `signature-invalid` | PASS |
| R7 | Wrong issuer public key | `signature-invalid` | PASS |

**Design decision (R4):** `Design` + populated `manufacture_meta` is an explicit **error**, not silently ignored.

## Happy-path coverage (verifier witness)

| ID | Case | Status |
|----|------|--------|
| A1 | Well-formed `Design` scope | PASS |
| A2 | Well-formed `Instance` scope | PASS |
| A3 | Well-formed `Batch` scope | PASS |
| A4 | `Design` + consistent `geo_anchor` | PASS |

## Issuance coverage (`BundleCertificationBrickTests`)

| Case | Status |
|------|--------|
| Issue `Design` / `Instance` / `Batch` → verifier accepts | PASS |
| Refuse `Instance` without `manufacture_meta` | PASS |
| Refuse `Design` with `manufacture_meta` | PASS |
| Refuse inconsistent `geo_anchor` at issuance | PASS |

## Witness independence

- Verifier tests use `WitnessBuilder` — hand-authored certs signed with witness keys via `PhysicalAtomCertificateSigning` only.
- Issuer tests use separate key material and `BundleCertificationBrick`; no shared fixtures with verifier suite.
- Malformed/tampered certs in R1/R6 are constructed by direct field mutation, not via issuance path.

## Execution

```bash
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  -f net8.0 \
  --filter "FullyQualifiedName~PhysicalAtomCertificateVerifierTests|FullyQualifiedName~BundleCertificationBrickTests"
```

All tests are headless (xUnit, no GUI/simulator/device).

## Phase 0 gate

Verifier rejects every malformed/tampered cert in the suite and accepts well-formed certs across all three `binding_scope` values. **17/17 tests passing** (11 verifier + 6 issuer).
