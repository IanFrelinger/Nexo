# Autonomous release manager

Ashlar's autonomous release manager has one job: produce a defensible
**READY** or **BLOCKED** verdict for one exact commit and version. It does not
publish packages, create tags, push images, or change production settings.
Those remain explicit operator actions.

The product host remains in
[github.com/IanFrelinger/ashlar-release-manager](https://github.com/IanFrelinger/ashlar-release-manager).
This repository owns the reusable audit engine and the checks it executes:

- `scripts/autonomous-release-manager.py` — coordinator and evidence writer
- `ci/autonomous-release-manager.json` — six required audit sub-agents
- `.github/workflows/autonomous-release-manager.yml` — weekly/manual execution

## Audit sub-agents

Every lane is mandatory and release-blocking. The coordinator hard-codes the
canonical set, so editing the plan cannot quietly remove or make one optional.

| Sub-agent | Scope |
|-----------|-------|
| `code` | Full Release build and open/commercial dependency boundaries |
| `tests` | Certification, complete CLI, and product suites |
| `security` | Trust/auth gates, supply chain, vulnerability scan, zero-egress container proof |
| `packaging` | Exact-version local pack, isolated consumer, external product shape |
| `documentation` | Published pins, public API/UAT promises, onboarding conventions |
| `operations` | Full-stack readiness, portal and agent-server dry runs, disaster recovery |

Lanes run in detached git worktrees at the same SHA, at most two at once. That
is not just an optimization: pack, restore, and coverage commands mutate build
outputs, so parallel audits in one checkout can manufacture false failures or
false passes. Lanes that share a host-global resource can also declare an
exclusive resource lock; the default security and operations lanes both lock
`docker`, so Compose/container tests never collide even while unrelated lanes
continue.

Within a lane, every step runs even after an earlier failure so the report
collects the full blocker set. A non-zero exit, timeout, missing executable,
coordinator exception, omitted lane, dirty tracked worktree, or version
inconsistency is a blocker.

## Run it

The candidate version must already be present in the root `VERSION` file.
`ci/published-version` stays at the last verified public release until
publication succeeds.

```bash
# Validate that all mandatory lanes and safety constraints are present.
make release-manager-validate

# Run the complete campaign on the current commit.
make release-manager-audit

# Equivalent explicit form; this refuses unless VERSION is exactly 0.2.0.
python3 scripts/autonomous-release-manager.py --version 0.2.0
```

Reports are written under:

```text
.ashlar/release-manager/runs/<utc>-<sha>/
  report.json
  report.md
  logs/
    code.log
    tests.log
    security.log
    packaging.log
    documentation.log
    operations.log
```

`latest.json` and `latest.md` are copies of the most recent result. Every lane
record includes the command, timeout, exit code, duration, log path, and log
SHA-256. `report.json` is the interface an external Release Manager host,
dashboard, or CI policy should consume.

The scheduled workflow publishes the report and lane logs as an Actions
artifact even when the verdict is blocked. A missing report is itself rendered
as **BLOCKED** in the workflow summary.

## Semantic adversarial review

Deterministic commands prove builds and known invariants; they cannot decide
whether a product claim is misleading or whether two correct-looking modules
compose unsafely. For a release candidate, the coordinating AI agent must fan
out the same six scopes to independent adversarial reviewers and then:

1. require exact file/runtime evidence for every finding;
2. reconcile duplicate or contradictory findings;
3. implement repository-owned fixes;
4. rerun affected lane commands;
5. run the deterministic coordinator on the final SHA;
6. refuse READY while either semantic blockers or deterministic blockers remain.

Semantic reviewers may propose and verify fixes, but they do not weaken the
plan, waive a blocker, or publish. A waiver belongs in `docs/exceptions.yaml`
with owner, expiry, mitigation, and sign-off; High/Critical release exceptions
remain blockers under the existing RC policy.

## Safety policy

The plan accepts argument arrays, not shell strings. Inline shell code and
known publishing commands (`git push`, `git tag`, `gh release`, NuGet/Docker
publish commands, and equivalents) are rejected during plan validation.
Repository scripts are allowed because they are reviewable and versioned.

The manager deliberately reports **BLOCKED** when `VERSION` still equals
`ci/published-version` while `[Unreleased]` contains work. Preparing a release
means first creating a release commit with the intended version and changelog;
auditing an arbitrary future version and tagging afterward is not accepted.

## Promotion remains manual

A READY report is evidence, not authorization. The operator still reviews the
report, signs off the exact SHA, and explicitly pushes the release tag described
in [`RELEASE_RUNBOOK.md`](RELEASE_RUNBOOK.md). The release workflow then
performs its own version guard and post-publish verification.
