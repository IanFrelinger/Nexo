# LAND-PLAN — Phase-3 + Self-Extend stacks (master `bc4e0e1`)

**Generated:** 2026-06-24  
**Authority:** GitHub PR / check-runs API (not local runs, not committed run-links)  
**Master:** `bc4e0e1c` — **unchanged** by this sprint  
**Dry-run branch:** `integration/land-dryrun` (local only, deleted after this doc)

---

## 1. Stack topology

Both stacks branch from `master` @ `bc4e0e1`. Git ancestry within each stack is linear (each tip contains all ancestors).

### Phase-3 (Certification.Composition)

```
master (bc4e0e1)
  └── cursor/agent-composer-proposer-6118      PR #195  (P3-S1)
        └── cursor/real-model-composer-6118    PR #196  (P3-S2)
              └── cursor/acceptance-rate-measurement-6118  PR #197  (P3-S3)
                    └── cursor/repo-hygiene-cleanup-6118   PR #198  (P3-CLEANUP) ← TIP
```

**GitHub base:** all four PRs target **`master`** (not parent-branch chained). Merging the tip (`repo-hygiene-cleanup`) lands S1–S3 + cleanup in one shot; PRs #195–#197 must be **closed manually** (or merged base-up if you want per-sprint PR closure).

### Self-extend (BackgroundAgents)

```
master (bc4e0e1)
  └── cursor/self-extend-audit-6118          PR #199
        └── cursor/self-extend-enforce-6118    PR #200
              └── cursor/self-extend-harden-6118  PR #201 ← TIP
```

**GitHub base:** properly stacked (child → parent). Landing requires merging **bottom-up** (#201 → #200 → #199) so each PR's base branch absorbs the next layer before the final merge to `master`.

### Shared touch-point

| File | Phase-3 | Self-extend | Dry-run conflict |
|------|---------|-------------|------------------|
| `scripts/cert-gate-config.sh` | **Modified** (P3 test inventory comments) | Unchanged | **None** |

Trees are otherwise disjoint (`Certification.Composition` vs `BackgroundAgents`).

---

## 2. PR topology + check-runs (API)

**Workflow mapping (stack-relevant gates):**

| Gate | Workflow | Covers |
|------|----------|--------|
| `cert-gate` | `.github/workflows/cert-gate.yml` → `scripts/run-cert-gate.sh` | Phase-3 composition tests + all certification hermetic tests |
| `kernel-gate` | `.github/workflows/kernel-gate.yml` → `make kernel-gate` | Kernel/hosting/pipeline (not BackgroundAgents directly) |
| `application-gate` | `.github/workflows/application-gate.yml` | Application/CLI/integration tier (includes BackgroundAgents paths on SX PRs) |
| `cross-platform-tests` | `.github/workflows/cross-platform-tests.yml` | BackgroundAgents cross-platform (informational on several PRs; not stack-blocking below) |

| PR | Branch | Base | Head | Mergeable | GH state | cert-gate | kernel-gate | application-gate | Stack gate verdict |
|----|--------|------|------|-----------|----------|-----------|-------------|------------------|-------------------|
| [#195](https://github.com/IanFrelinger/Nexo/pull/195) | `cursor/agent-composer-proposer-6118` | `master` | `dbd87e79` | MERGEABLE | BLOCKED† | **success** | **success** | n/a | ✅ **GREEN** (cert-gate) |
| [#196](https://github.com/IanFrelinger/Nexo/pull/196) | `cursor/real-model-composer-6118` | `master` | `aecda581` | MERGEABLE | BLOCKED† | **success** | **success** | n/a | ✅ **GREEN** (cert-gate) |
| [#197](https://github.com/IanFrelinger/Nexo/pull/197) | `cursor/acceptance-rate-measurement-6118` | `master` | `31f5a945` | MERGEABLE | BLOCKED† | **success** | **success** | n/a | ✅ **GREEN** (cert-gate) |
| [#198](https://github.com/IanFrelinger/Nexo/pull/198) | `cursor/repo-hygiene-cleanup-6118` | `master` | `9420747d` | MERGEABLE | BLOCKED† | **success** | **success** | n/a | ✅ **GREEN** (cert-gate) |
| [#199](https://github.com/IanFrelinger/Nexo/pull/199) | `cursor/self-extend-audit-6118` | `master` | `a8491640` | MERGEABLE | BLOCKED† | **success** | n/a | n/a | ✅ **GREEN** (cert-gate) |
| [#200](https://github.com/IanFrelinger/Nexo/pull/200) | `cursor/self-extend-enforce-6118` | `self-extend-audit-6118` | `24d0fbb5` | MERGEABLE | UNSTABLE‡ | **success** | **success** | n/a | ✅ **GREEN** (cert-gate + kernel-gate) |
| [#201](https://github.com/IanFrelinger/Nexo/pull/201) | `cursor/self-extend-harden-6118` | `self-extend-enforce-6118` | `e9438994` | MERGEABLE | UNSTABLE‡ | **success** | **success** | **success** | ✅ **GREEN** (all three) |

† **BLOCKED** on Phase-3 / audit PRs: non-stack checks failing (`lychee`, `Pack script vs Nexo.Hosting graph`, `Standalone brick authoring scaffold`, cross-platform `Linux/macOS/Windows — setup`, etc.). **Stack gates (`cert-gate`) are green on all seven PRs.**

‡ **UNSTABLE** on enforce/harden: `kernel-coverage` failure + assorted informational workflow failures. **Stack gates (`cert-gate`, `kernel-gate`, `application-gate` on #201) are green.**

> Branch-protection required-context list was not readable via API (403). Gate selection above follows sprint scope: `cert-gate` for Phase-3; `cert-gate` + `kernel-gate` + `application-gate` for self-extend production changes.

---

## 3. Dry-run integration result

**Branch:** `integration/land-dryrun` off `origin/master` (`bc4e0e1`)  
**Strategy:** merge-commit (not squash) — preserves per-sprint history

```bash
git checkout -B integration/land-dryrun origin/master
git merge --no-edit origin/cursor/repo-hygiene-cleanup-6118    # Phase-3 tip — fast-forward
git merge --no-edit origin/cursor/self-extend-harden-6118      # Self-extend tip — merge commit 72bf5df7
```

| Step | Result |
|------|--------|
| Conflicts | **None** (`cert-gate-config.sh` touched only by Phase-3; clean 3-way merge) |
| `bash scripts/run-cert-gate.sh` | **PASS** — 41/41 tests, exit 0 |
| `dotnet test src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj -f net8.0` | **PASS** — 423 passed, 1 skipped (invariant D), 0 failed |
| Combined diff vs master | 72 files, +3579 / −194 lines |

**Verdict: ✅ INTEGRATION CLEAN + GREEN** — safe to prescribe landing both stacks.

---

## 4. Recommended merge order

Land **Phase-3 first**, then **self-extend** (matches dry-run; disjoint trees; either order should work, but this is proven).

### 4A. Phase-3 → `master` (pick one strategy)

**Option A — Tip merge (one PR, fastest):** merge only PR #198; manually close #195–#197 as superseded.

**Option B — Base-up (four PRs, per-sprint PR closure):** merge #195 → #196 → #197 → #198 in order (each into `master`).

Both are equivalent git content when branches are stacked as verified.

### 4B. Self-extend → `master` (must be base-up)

Merge #201 into `enforce` → #200 into `audit` → #199 into `master`. Merging only the harden tip directly to `master` (bypassing stacked PRs) works git-wise but leaves #199/#200 open and skips the intended review chain.

---

## 5. Human-runnable command sequence

> **Legend:** lines marked **⛔ IRREVERSIBLE** mutate remote shared state.

### Step 0 — Preflight (read-only)

```bash
git fetch origin
git rev-parse origin/master   # expect bc4e0e1c9a6ba4b6f212311bc5d7f5e601aea413
gh pr checks 198 --repo IanFrelinger/Nexo | grep cert-gate
gh pr checks 201 --repo IanFrelinger/Nexo | grep -E 'cert-gate|kernel-gate|application-gate'
```

### Step 1 — Land Phase-3 (choose A or B)

#### Option A: tip merge (recommended)

```bash
gh pr merge 198 --repo IanFrelinger/Nexo --merge --subject "Merge Phase-3 composition stack (S1–S3 + cleanup)"  # ⛔ IRREVERSIBLE — writes master
# Close superseded PRs without merging (commits already on master):
gh pr close 195 --repo IanFrelinger/Nexo --comment "Superseded by #198 tip merge"
gh pr close 196 --repo IanFrelinger/Nexo --comment "Superseded by #198 tip merge"
gh pr close 197 --repo IanFrelinger/Nexo --comment "Superseded by #198 tip merge"
```

#### Option B: base-up (four merge commits on master)

```bash
gh pr merge 195 --repo IanFrelinger/Nexo --merge   # ⛔ IRREVERSIBLE
gh pr merge 196 --repo IanFrelinger/Nexo --merge   # ⛔ IRREVERSIBLE
gh pr merge 197 --repo IanFrelinger/Nexo --merge   # ⛔ IRREVERSIBLE
gh pr merge 198 --repo IanFrelinger/Nexo --merge   # ⛔ IRREVERSIBLE
```

### Step 2 — Land self-extend (stacked, base-up)

```bash
gh pr merge 201 --repo IanFrelinger/Nexo --merge --subject "Merge SX-HARDEN into enforce"   # ⛔ IRREVERSIBLE (updates enforce branch)
gh pr merge 200 --repo IanFrelinger/Nexo --merge --subject "Merge SX-ENFORCE into audit"    # ⛔ IRREVERSIBLE (updates audit branch)
gh pr merge 199 --repo IanFrelinger/Nexo --merge --subject "Merge self-extend stack to master"  # ⛔ IRREVERSIBLE — writes master
```

After step 2, `master` contains both stacks. GitHub auto-closes #200 and #201 when their commits reach `master` via the chain.

### Step 3 — Post-land verification

```bash
git fetch origin
git checkout master && git pull origin master
bash scripts/run-cert-gate.sh
dotnet test src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj -f net8.0
```

### Step 4 — Branch protection + auto-delete (GitHub UI or API)

**Settings → Branches → `master` → Branch protection:**

1. Require status checks: add **`cert-gate`** (minimum for composition land).
2. Optionally add **`kernel-gate`** and **`application-gate`** now that BackgroundAgents enforcement is on master.
3. Enable **“Automatically delete head branches”** after merge.

```bash
# If using gh api (requires admin PAT):
# gh api repos/IanFrelinger/Nexo/branches/master/protection ...   # ⛔ IRREVERSIBLE — changes branch policy
```

### Step 5 — Archive tags (before any branch deletion)

Run **before** deleting any cursor branch. Archive tags are cheap insurance against squash-merge ancestry loss.

```bash
git fetch origin

for b in \
  cursor/agent-composer-proposer-6118 \
  cursor/real-model-composer-6118 \
  cursor/acceptance-rate-measurement-6118 \
  cursor/repo-hygiene-cleanup-6118 \
  cursor/self-extend-audit-6118 \
  cursor/self-extend-enforce-6118 \
  cursor/self-extend-harden-6118
do
  tag="archive/landed-${b#cursor/}"
  git tag "$tag" "origin/$b"
  git push origin "refs/tags/$tag"    # ⛔ IRREVERSIBLE — creates remote tag (safe; not destructive)
done
```

### Step 6 — Branch cleanup (separate human step; do NOT run until archive tags exist)

```bash
# Only after archive tags pushed and PRs confirmed merged/closed:
git push origin --delete cursor/agent-composer-proposer-6118      # ⛔ IRREVERSIBLE
git push origin --delete cursor/real-model-composer-6118        # ⛔ IRREVERSIBLE
git push origin --delete cursor/acceptance-rate-measurement-6118  # ⛔ IRREVERSIBLE
git push origin --delete cursor/repo-hygiene-cleanup-6118       # ⛔ IRREVERSIBLE
git push origin --delete cursor/self-extend-audit-6118          # ⛔ IRREVERSIBLE
git push origin --delete cursor/self-extend-enforce-6118        # ⛔ IRREVERSIBLE
git push origin --delete cursor/self-extend-harden-6118         # ⛔ IRREVERSIBLE
```

---

## 6. What this sprint did NOT do

- Did **not** merge, rebase, or force-push `master` (still `bc4e0e1`)
- Did **not** push `integration/land-dryrun` to origin
- Did **not** delete any remote branch
- Did **not** resolve conflicts by discarding either stack (none existed)

---

## 7. Dry-run cleanup (agent)

```bash
git checkout cursor/self-extend-harden-6118   # or master
git branch -D integration/land-dryrun         # local scratch only
```

`integration/land-dryrun` was never pushed.
