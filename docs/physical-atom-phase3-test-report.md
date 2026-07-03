# Physical-Atom Phase 3 — Test Report

**Sprint:** Phase 3 — HTTP Resolution + Tag Verify Orchestration  
**Maturity:** Prototype

## Orchestrator rejection (`PhysicalAtomTagVerifyOrchestratorTests`)

| ID | Case | Failure code | Status |
|----|------|--------------|--------|
| R1 | Malformed QR | `tag-prefix-invalid` | PASS |
| R2 | Unregistered atom | `atom-unresolved` | PASS |
| R3 | Tag hash ≠ registered cert | `tag-reference-mismatch` | PASS |
| R4 | Wrong issuer fingerprint | `tag-issuer-fingerprint-mismatch` | PASS |
| A1 | Valid QR + populated store | trusted | PASS |

## HTTP router (`HttpAssetResolutionRouterTests`)

| Case | Status |
|------|--------|
| Unknown route → 404 | PASS |
| Unregistered cert → 404 | PASS |
| GET cert → 200 JSON | PASS |
| GET asset → 200 bytes | PASS |

## End-to-end (`PhysicalAtomEndToEndFlowTests`)

| Case | Status |
|------|--------|
| Pipeline → HTTP resolve → tag verify | PASS |

**10/10 Phase 3 tests** — cert-gate total **99**.
