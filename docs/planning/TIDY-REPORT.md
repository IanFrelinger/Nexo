# TIDY-REPORT — Post-landing cleanup (6118 arc)

**Generated:** 2026-06-25  
**Repo:** [IanFrelinger/Ashlar](https://github.com/IanFrelinger/Nexo)  
**Master HEAD:** `f3be445848e24785b64ea5f7580998884647ea5a` — `chore(repo): P3-CLEANUP hygiene pass — test doubles relocated, evidence consolidated (#198)`

---

## 1. Master-green gate (HARD STOP)

| Field | Value |
|-------|-------|
| Commit | `f3be445848e24785b64ea5f7580998884647ea5a` |
| API | `GET /repos/IanFrelinger/Ashlar/commits/f3be445848e24785b64ea5f7580998884647ea5a/check-runs?check_name=cert-gate` |
| **cert-gate `total_count`** | **0** |
| **cert-gate `conclusion`** | **ABSENT** (no check run registered on master HEAD) |
| **Gate result** | **FAIL — sprint stopped; no branches pruned** |

### Why cert-gate is absent on master

The `Cert gate` workflow (`.github/workflows/cert-gate.yml`) triggers only on `pull_request` and `workflow_dispatch` — **not** on `push` to `master`. After squash-merge of PR #198, no `cert-gate` check run was created on the merge commit `f3be445`.

**Nearest green cert-gate evidence (PR head, not master):**

| Field | Value |
|-------|-------|
| PR #198 head | `04ffd8cccab7b60a9fce9a50c844ce4fc0839df2` |
| cert-gate conclusion | `success` |
| Run | [actions/runs/28137102010](https://github.com/IanFrelinger/Nexo/actions/runs/28137102010/job/83326229005) |

### Required human action before pruning

Dispatch cert-gate on master (or add `push: branches: [master]` to the workflow), then re-run this tidy sprint:

```bash
# Option A — one-off workflow dispatch on master
gh workflow run cert-gate.yml --ref master

# Option B — after dispatch completes, verify:
gh api 'repos/IanFrelinger/Ashlar/commits/$(gh api repos/IanFrelinger/Ashlar/commits/master --jq .sha)/check-runs?check_name=cert-gate' \
  --jq '.check_runs[] | {name, conclusion, status}'
```

Until `cert-gate` reports `conclusion: success` on master HEAD, **do not delete arc branches**.

---

## 2. Per-branch verification table

Evidence method: GitHub PR API (`merged` field) + `refs/tags/archive/landed-<name>-6118` pointing at branch tip (`object.type == commit`, SHA match). No `git branch --merged` / `git cherry` used.

| Branch | PR# | PR merged? | Archive tag? | Tag = tip? | DECISION |
|--------|-----|------------|--------------|------------|----------|
| `cursor/agent-composer-proposer-6118` | #195 | **no** (closed, `merged: false`) | yes (`archive/landed-agent-composer-proposer-6118` → `dbd87e79`) | yes | **SKIP** — PR not merged per API |
| `cursor/real-model-composer-6118` | #196 | **no** (closed, `merged: false`) | yes (`archive/landed-real-model-composer-6118` → `aecda581`) | yes | **SKIP** — PR not merged per API |
| `cursor/acceptance-rate-measurement-6118` | #197 | **no** (closed, `merged: false`) | yes (`archive/landed-acceptance-rate-measurement-6118` → `31f5a945`) | yes | **SKIP** — PR not merged per API |
| `cursor/repo-hygiene-cleanup-6118` | #198 | **yes** (`merged: true`, merge `f3be445`) | yes (`archive/landed-repo-hygiene-cleanup-6118` → `04ffd8cc`) | yes | **BLOCKED** — master gate fail (would DELETE when green) |
| `cursor/self-extend-audit-6118` | #199 | **yes** (`merged: true`, merge `74c473f7`) | yes (`archive/landed-self-extend-audit-6118` → `19ae0b93`) | yes | **BLOCKED** — master gate fail (would DELETE when green) |
| `cursor/self-extend-enforce-6118` | #200 | **yes** (`merged: true`, merge `19ae0b93`) | yes (`archive/landed-self-extend-enforce-6118` → `d1897628`) | yes | **BLOCKED** — master gate fail (would DELETE when green) |
| `cursor/self-extend-harden-6118` | #201 | **yes** (`merged: true`, merge `d1897628`) | yes (`archive/landed-self-extend-harden-6118` → `e9438994`) | yes | **BLOCKED** — master gate fail (would DELETE when green) |

### Deletions performed

**None.** Master-green gate failed before any `git push origin --delete` commands were issued.

### Post-gate delete commands (for human re-run)

When cert-gate is green on master HEAD, run only the four branches that pass both checks:

```bash
gh api -X DELETE repos/IanFrelinger/Ashlar/git/refs/heads/cursor/repo-hygiene-cleanup-6118
gh api -X DELETE repos/IanFrelinger/Ashlar/git/refs/heads/cursor/self-extend-audit-6118
gh api -X DELETE repos/IanFrelinger/Ashlar/git/refs/heads/cursor/self-extend-enforce-6118
gh api -X DELETE repos/IanFrelinger/Ashlar/git/refs/heads/cursor/self-extend-harden-6118
```

**Do not delete** PRs #195–#197 branches until their PRs show `merged: true` via the API (archive tags alone are insufficient).

---

## 3. `cursor/land-plan-6118` — special case

| Field | Value |
|-------|-------|
| Branch tip | `b506a51a1846c47c264f5a96e380ec57614719c9` |
| PR | none |
| Archive tag | none |
| `LAND-PLAN.md` on master | **no** |
| Action taken | **NOT deleted** (per sprint constraints) |

### Human choice (default: leave untouched)

| Option | Action |
|--------|--------|
| **A — Keep as record (recommended if doc has value)** | Open a small PR from `cursor/land-plan-6118` merging only `LAND-PLAN.md` to `master` as a landing audit trail. |
| **B — Discard later** | Create `archive/landed-land-plan-6118` tag at `b506a51a`, then delete the branch when ready. |

**Default:** leave branch and tag untouched; flag for maintainer decision.

---

## 4. Repo settings (branch protection + auto-delete)

Agent token lacks admin access to branch-protection endpoints (`403 Resource not accessible by integration`). Settings read back where possible; prescriptions below are **PENDING-HUMAN**.

### (a) Branch protection on `master` — require `cert-gate`

| Field | State |
|-------|-------|
| API read | `GET /repos/IanFrelinger/Ashlar/branches/master/protection` → **403** |
| Rulesets | `GET /repos/IanFrelinger/Ashlar/rulesets` → `[]` (empty) |
| **Status** | **PENDING-HUMAN** |

```bash
# Requires repo admin. Enables protection and requires cert-gate status check.
gh api -X PUT repos/IanFrelinger/Ashlar/branches/master/protection \
  --input - <<'EOF'
{
  "required_status_checks": {
    "strict": true,
    "checks": [
      {"context": "cert-gate", "app_id": null}
    ]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null,
  "required_linear_history": false,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "block_creations": false,
  "required_conversation_resolution": false
}
EOF

# Verify:
gh api repos/IanFrelinger/Ashlar/branches/master/protection \
  --jq '.required_status_checks.checks[] | select(.context == "cert-gate")'
```

> **Note:** Consider also adding `push` trigger to `cert-gate.yml` on `master` so the required check can actually run post-merge.

### (b) Auto-delete head branches on merge

| Field | State |
|-------|-------|
| API read | `GET /repos/IanFrelinger/Ashlar` → `delete_branch_on_merge: false` |
| **Status** | **PENDING-HUMAN** (confirmed off, not yet enabled) |

```bash
gh api -X PATCH repos/IanFrelinger/Ashlar \
  -f delete_branch_on_merge=true

# Verify:
gh api repos/IanFrelinger/Ashlar --jq '.delete_branch_on_merge'
# Expected: true
```

---

## 5. Untouched (per constraints)

- `master` content — unchanged
- All existing tags — unchanged (including `archive/landed-*`)
- `dependabot/*` branches — not queried, not touched
- `cursor/phase2-cross-project-reuse-6118` — not in arc delete list, not touched
- `cursor/land-plan-6118` — retained, flagged above
- PRs #195–#197 branches — retained (PR not merged per API)

---

## 6. Summary

| Criterion | Result |
|-----------|--------|
| cert-gate on master `f3be445` recorded from API | **ABSENT** (`total_count: 0`) — sprint stopped |
| Branches deleted | **0** |
| Every would-be delete had merged PR + archive tag | 4 candidates identified; 0 deleted (gate blocked) |
| `land-plan-6118` preserved + decision surfaced | yes |
| Settings confirmed or PENDING-HUMAN | both **PENDING-HUMAN** |
| Non-arc / dependabot / tags untouched | yes |
