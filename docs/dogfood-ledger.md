# Dogfood Ledger

**Purpose:** Dated pass/fail evidence for demos before any autonomy or design-partner marketing claim. This ledger records what actually runs end-to-end for Ashlar dogfood / shippable-demo truth. Product copy stays with Marketing/Product 3000.

For the full gate catalog and block test commands, see [`docs/DogfoodValidation.md`](DogfoodValidation.md).

## Entries

| Date | Demo | Pass/Fail | Gap | Owner | Repro |
|------|------|-----------|-----|-------|-------|
| 2026-09-05 | Cert-gate trust signature fail-closed (limitations 7–9) via PR #513 | **PASS (gate closed on master)** | Ledger bootstrap only; full end-to-end shippable demos still need dated entries. Autonomy marketing remains blocked until the ledger shows real E2E passes with repro. | Dogfood Ledger | [PR #513](https://github.com/IanFrelinger/Ashlar/pull/513) closed limitations 7–9; merge commit `16125e58f098826fc1a970ce609fefe77759b5d4`; `CertificationForgeAttackTests` (signature stripping / schema downgrade / composition key); see [`docs/certification-evidence.md`](certification-evidence.md) limitations 7–9 marked CLOSED |
