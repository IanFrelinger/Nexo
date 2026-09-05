# Autonomous release manager

Ashlar's autonomous release manager has one job: produce a defensible
**READY** or **BLOCKED** verdict for one exact commit and version. It does not
publish packages, create tags, push images, or change production settings.
Those remain explicit operator actions.

The product host remains in
[github.com/IanFrelinger/ashlar-release-manager](https://github.com/IanFrelinger/ashlar-release-manager).
It is currently a nuget.org/lifecycle reference and does **not** consume this
report yet; wiring its next package update to `latest.json` is a separate
product-repository change. The repo-local manager here is the release authority
for Ashlar itself.
This repository owns the reusable audit engine and the checks it executes:

- `.cursor/agents/release-manager.md` — mutable coordinator persona
- `.cursor/agents/*-auditor.md` — six read-only semantic specialists
- `.cursor/skills/release-manager/SKILL.md` — reusable orchestration playbook
- `.cursor/rules/release-publishing-safety.mdc` — always-on publishing boundary
- `scripts/autonomous-release-manager.py` — coordinator and evidence writer
- `ci/autonomous-release-manager.json` — six required audit sub-agents
- `.github/workflows/autonomous-release-manager.yml` — weekly/manual execution

## Audit sub-agents

Every lane is mandatory and release-blocking. The coordinator hard-codes the
canonical set, so editing the plan cannot quietly remove or make one optional.
Plan validation also requires all committed specialist definitions, the
release-manager skill, and the always-on publishing-safety rule.

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

`latest.json` and `latest.md` are atomic pointers to the most recent complete
report directory; evidence paths remain relative to the report that owns them.
Every lane record includes the command, timeout, exit code, duration, log path,
and log SHA-256. `report.json` is the future interface an external Release
Manager host, dashboard, or CI policy should consume.

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

The Linux release plan accepts argument arrays, not shell strings. Inline shell
code and publishing commands (`git push`, `git tag`, `gh release`,
NuGet/Docker publish commands, and equivalents) are rejected during plan
validation. The exact tracked plan is SHA-256-bound to the coordinator, loaded
from the audited commit, and runs with inherited Ashlar controls and credential
environment variables removed. Step `environment` maps cannot override
coordinator-owned variables (`PATH`, `HOME`, `DOTNET_*`, `GIT_*`, shell hooks,
and the audit token). Repository scripts are allowed because they are
reviewable and versioned; this policy complements OS/credential isolation and
is not presented as a sandbox for arbitrary scripts.

Any repository blocker — dirty tree, SemVer/changelog inconsistency, downgrade,
or a missing dated release section — skips lane execution. The report is still
written as **BLOCKED**. Preparing a release means first creating a release
commit with the intended version and changelog; auditing an arbitrary future
version and tagging afterward is not accepted.

`latest.json` records the run id, commit, version, verdict, and the plan and
coordinator SHA-256 values from that report.

## Promotion remains manual

A READY report is evidence, not authorization. The operator still reviews the
report, signs off the exact SHA, and explicitly pushes the release tag described
in [`RELEASE_RUNBOOK.md`](RELEASE_RUNBOOK.md). The release workflow then
performs its own version guard and post-publish verification.
