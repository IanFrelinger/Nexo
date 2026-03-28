# Release Candidate Checklist v1

Use this checklist to move from "locally passing" to "release-ready with evidence."

## 1) CI gate (mandatory)

- [ ] Trigger `production-readiness-gate-v1` in GitHub Actions.
- [ ] Trigger `environment-setup-gate-v1` in GitHub Actions.
- [ ] Trigger `container-image-gate` in GitHub Actions.
- [ ] Trigger `container-image-publish` in GitHub Actions (or verify latest successful publish on `master`).
- [ ] Trigger `onboarding-docs-guard` in GitHub Actions.
- [ ] Confirm ephemeral setup container jobs pass for each distro in matrix.
- [ ] Confirm matrix jobs pass on:
  - [ ] ubuntu-latest
  - [ ] windows-latest
  - [ ] macos-latest
- [ ] Confirm uploaded artifacts include:
  - [ ] test TRX files
  - [ ] `gate-validate.log`
  - [ ] `gate-run-success.log`
  - [ ] `gate-run-fallback.log`
  - [ ] `gate-diagnostics.log`
  - [ ] `gate-resume-source.log`
  - [ ] `gate-resume-target.log`
  - [ ] `setup-gate-summary-<os>.txt`
  - [ ] `setup-gate-ephemeral-summary-*.txt`
  - [ ] container image gate summary + smoke logs
  - [ ] published image smoke log (`docker run ... --help` in publish workflow)

## 2) Runtime correctness review

- [ ] Verify `gate-run-fallback.log` shows `hybrid` stage worker type as `Agentic`.
- [ ] Verify `gate-resume-source.log` has run state `Failed` (intentional source failure).
- [ ] Verify `gate-resume-target.log` has run state `Completed`.
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
