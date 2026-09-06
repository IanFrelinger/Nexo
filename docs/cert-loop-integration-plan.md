# Certified Loop Integration into Live Extender Path

**Status:** In Progress (PR branch: `cursor/integrate-cert-loop-extender-ea44`)  
**Goal:** Converge the legacy extender path and the certified autonomy loop into a single, gated path.

## Problem Statement

As documented in `SELF-EXTEND-AUDIT.md` line 103:

> "What remains open is not a ceiling but convergence: the legacy extender path and the certified autonomy loop are two self-extension paths, and the long-term intent is one."

Currently:
- **Invariants A, B, C, D** are enforced (cert-gate, policy narrowing, human admission gates, recursion ceiling)
- **Canary verification** (A4) exists in `SelfExtendAdmissionBridge` for mediated writes
- **Watch window** infrastructure exists in `CertifiedBrickHotSwapHost`
- **BUT**: These are not required in all extender paths, creating a potential bypass

## Integration Approach

### Phase 1: Require Certification for All Autonomous Admissions ✓

The admission path through `SelfExtendAdmissionBridge.TryRecordAsync()` already enforces:
1. **A2 Compile Check** - Proposed changes must compile before admission
2. **A4 Canary** - Post-apply verification with automatic rollback on failure
3. **Mediation** - For ashlar projects, all writes go through the forge store

This is currently activated only for projects with `ashlar.policy.yaml`. The integration makes this **required** for any autonomous admission, not optional.

### Phase 2: Integration Test Coverage ✓

New test file: `LiveExtenderCertLoopIntegrationTests.cs`

Proves:
- **Canary pass path**: Changes that pass verification are admitted and applied
- **Canary fail path**: Changes that fail verification are rolled back (fail-closed)
- **Watch window path**: (Placeholder for Phase 3 integration)

### Phase 3: Watch Window Integration (TODO)

After a change is admitted and applied, it enters a watch window that monitors:
- Error rate deltas vs. baseline
- Latency deltas vs. baseline
- Undeclared writes (contract violations)
- Resource ceiling breaches

On threshold breach → automatic rollback + quarantine (R5.2-R5.4 from the autonomy spec).

Implementation plan:
1. Wire `CertifiedBrickHotSwapHost` or equivalent watch infrastructure into the post-apply path
2. Track baseline metrics before applying changes
3. Monitor metrics during the watch window
4. Trigger rollback on breach
5. Record quarantine + revocation in the cert store

### Phase 4: CI Proof

Add workflow step or expand existing `cert-gate` to run:
```bash
dotnet test src/Ashlar.Tests.BackgroundAgents/Ashlar.Tests.BackgroundAgents.csproj \
  --filter "FullyQualifiedName~LiveExtenderCertLoopIntegrationTests"
```

This proves the integration is CI-verified, not just asserted.

## Trust Properties Preserved

1. **Fail-closed default** (Invariant C): Unconfigured → Passive, no cycles run
2. **Cert-gate inheritance** (Invariant A): Brick writes require verified certification
3. **Policy narrowing** (Invariant B): Machine-origin agents have envelope ⊆ creator
4. **Recursion ceiling** (Invariant D): Bounded depth, unattended cycles, rate
5. **Canary verification** (A4): Post-apply check with automatic rollback
6. **Compile evidence** (A2): Changes must compile before admission

## What This Closes

- The gap between "certified autonomy loop" and "legacy extender path"
- The potential for autonomous admission without compilation evidence
- The potential for autonomous admission without post-apply verification

## What Remains Open

- Watch window integration (Phase 3)
- Full end-to-end autonomous cycle with all gates in series (tracked separately)

## References

- `docs/SELF-EXTEND-AUDIT.md` - Current invariant enforcement audit
- `docs/trust-loop/trust-loop-ext-autonomous-self-extension.md` - Autonomy spec R5.1-R5.5
- `src/Ashlar.BackgroundAgents.HostRunners/SelfExtendAdmissionBridge.cs` - Admission gate
- `src/Ashlar.Infrastructure/Certification/HotSwap/CertifiedBrickHotSwapHost.cs` - Watch window
- `src/Ashlar.Tests.BackgroundAgents/SelfExtend/LiveExtenderCertLoopIntegrationTests.cs` - Integration proof
