# Release Candidate Checklist v1

Use this checklist to move from "locally passing" to "release-ready with evidence."

## 1) CI gate (mandatory)

- [ ] Trigger `production-readiness-gate-v1` in GitHub Actions.
- [ ] Trigger `environment-setup-gate-v1` in GitHub Actions.
- [ ] Trigger `runtime-release-gate` in GitHub Actions (core + visual required, chaos non-gating).
- [ ] Do **not** treat `runtime-release-promotion` as mandatory: `docs/CiGateInventory.md` records it as manual-only / historically red. Required release proof is `runtime-release-gate` plus an autonomous release-manager **READY** verdict on the candidate SHA.
- [ ] Trigger `installer-bruteforce-gate` in GitHub Actions.
- [ ] Trigger `container-image-gate` in GitHub Actions.
- [ ] Trigger `container-image-publish` only on explicit `workflow_dispatch` if a rolling `:latest` image is required. Versioned images stay on READY-gated `release.yml`.
- [ ] Trigger `onboarding-docs-guard` in GitHub Actions.
- [ ] Confirm ephemeral setup container jobs pass for each distro in matrix.
- [ ] Confirm matrix jobs pass on:
  - [ ] ubuntu-latest
  - [ ] windows-latest
  - [ ] macos-latest
- [ ] Confirm uploaded artifacts include:
  - [ ] test TRX files
  - [ ] `gate-validate.log`
  - [ ] `gate-run-unconfigured.log`
  - [ ] `gate-run-hooks.log`
  - [ ] `gate-diagnostics.log`
  - [ ] `gate-resume-source.log`
  - [ ] `gate-resume-target.log`
  - [ ] `setup-gate-summary-<os>.txt`
  - [ ] `setup-gate-ephemeral-summary-*.txt`
  - [ ] container image gate summary + smoke logs
  - [ ] published image smoke log (`docker run ... --help` in publish workflow)
  - [ ] runtime release lane logs (core, visual, chaos)
  - [ ] runtime SLO evidence JSON (`.ashlar/runtime/release-gate/last-run/evidence.json`)
  - [ ] runtime SLO evidence markdown (`.ashlar/runtime/release-gate/last-run/evidence.md`)
  - [ ] installer brute-force matrix + summary logs
  - [ ] native installer package artifacts (linux/macos/windows)

## 2) Runtime correctness review

- [ ] Verify `gate-run-unconfigured.log` is fail-closed (`ok=false`, `state=Failed`, ingest names the unconfigured placeholder).
- [ ] Verify `gate-run-hooks.log` stays fail-closed (test hooks must not fabricate success).
- [ ] Verify `gate-resume-source.log` has run state `Failed` (intentional source failure).
- [ ] Verify `gate-resume-target.log` stays `Failed` and does not report `no prior run was found`.
- [ ] Verify `gate-diagnostics.log` reports known persistence provider and resolved adapter keys.

## 3) Exceptions policy (mandatory)

For each open High/Critical exception:

- [ ] Owner assigned
- [ ] Expiration date set
- [ ] Mitigation plan documented
- [ ] Explicit sign-off recorded

If any item is missing for any High/Critical exception, release is blocked.

## 4) Rollback readiness (mandatory)

- [ ] Rollback command/procedure documented.
- [ ] Rollback tested in staging or equivalent environment.
- [ ] Responsible operator/team identified.

## 5) Release decision

- [ ] All sections above complete.
- [ ] Product/engineering sign-off recorded.
- [ ] Release candidate promoted.
