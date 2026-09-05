---
name: release-manager
description: Orchestrate end-to-end release readiness with six specialist auditors, fix verified blockers, and produce deterministic evidence without publishing.
icon: rocket
color: orange
---

# Release Manager

Use this skill for release preparation, release audits, or requests to make a
candidate release-ready.

## Invariants

- Bind every review and command to one clean commit SHA.
- Use exactly six specialist scopes: code, CI/tests, security, packaging,
  documentation, and operations.
- Missing, skipped, cancelled, timed-out, malformed, or zero-test evidence is
  a blocker.
- Prefer independent parallel specialist reviews. Reconcile results in the
  parent; specialists do not delegate another level.
- Fix only verified repository-owned defects. Re-run affected scopes after
  every fix.
- Do not weaken `ci/autonomous-release-manager.json` to get a green verdict.
- Never publish, deploy, upload release assets, create/push tags or releases,
  or change production/repository settings without explicit user
  authorization for that exact action.

## Workflow

1. Resolve the release scope:
   - candidate SHA;
   - candidate version;
   - supported deployment/product surfaces;
   - last published version.
2. Run `make release-manager-validate`.
3. Launch these project subagents, preferably in parallel:
   - `/code-auditor`
   - `/ci-auditor`
   - `/security-auditor`
   - `/packaging-auditor`
   - `/documentation-auditor`
   - `/operations-auditor`
4. Consolidate findings:
   - preserve exact evidence;
   - merge duplicates;
   - call out disagreements;
   - separate stop-ship blockers from recommendations.
5. Implement stop-ship fixes in small commits and run proportionate focused
   tests after each commit.
6. Repeat affected specialist reviews until they return no release blockers.
7. Run the deterministic campaign:

   ```bash
   python3 scripts/autonomous-release-manager.py
   ```

8. Read `.ashlar/release-manager/latest.json`, `latest.md`, and each failed
   lane log. READY requires:
   - all six semantic scopes clear;
   - all six deterministic lanes passed;
   - no repository-state blockers;
   - green CI on the same SHA;
   - current sign-off and rollback evidence.
9. Stop at READY and ask for an explicit publishing instruction. Do not infer
   authorization from readiness.

## Evidence handoff

Include:

- exact SHA/version;
- READY or BLOCKED;
- blocker list with owner/action;
- commands and outcomes;
- report/log artifact paths and SHA-256 values;
- known limitations and supported release scope;
- PR link when code changed.

The deterministic coordinator and full plan are documented in
`docs/AutonomousReleaseManager.md`.
