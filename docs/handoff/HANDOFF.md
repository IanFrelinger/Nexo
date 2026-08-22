# Handoff — game-layer extraction and the Ashlar rename

**Revision 2.** Revision 1 was written in a cloud session with **no .NET SDK**, so
nothing in it had been compiled. It has now been run on a machine with the SDK.
Three of its load-bearing claims were wrong, and two of them would have caused
damage. This revision records what was measured, not what was inferred.

---

## 1. Decisions (unchanged)

| Decision | Answer |
|---|---|
| **Rename scope** | **Full rename.** `Nexo` → `Ashlar` across namespaces, assemblies, packages, directories and filenames. |
| **Game code destination** | **Its own repository**, consuming the kernel as a package. |

Reference material from the originating session — both written pre-rename, both
still say "Nexo" throughout:

- **The Nexo Dossier** — source map, usage modes, positioning, and an audited list
  of where documentation and code disagree.
  <https://claude.ai/code/artifact/a1b2328c-05ac-4fad-b7c5-283ee07ebf06>
- **Nexo Cleanup Plan** — seven waves with file-level work items.
  <https://claude.ai/code/artifact/bcfebaf4-6b03-42df-8e8d-0ca2c6424689>

---

## 2. What revision 1 got wrong

### 2.1 "Tier 1 cannot break a kernel build" — false. It breaks it.

Revision 1 classified the Playtest tree and `TileMapRenderTool.cs` as Tier 1:
*self-contained game code with no inbound references from kernel production code.*

Applied in a throwaway worktree and built exactly as revision 1 documented:

```
Build FAILED.  14 Error(s)

AgentFactory.cs(7,33):  CS0234  namespace 'Playtest' does not exist in 'Nexo.Orchestration.Agents'
AgentFactory.cs(12,26): CS0234  namespace 'Playtest' does not exist in 'Nexo.Orchestration'
ToolsDevTests.cs:       CS0246  'TileMapRenderTool' could not be found   (x10)
```

`AgentFactory.CreateAgent` calls `IsPlaytestDomain(spec.Domain)` and then
`CreatePlaytestAgent(spec)`, which switches on `"aiplayer"` / `"playtest"` /
`"balance"` / `"feedback"` and news up `AIPlayerAgent`, `BalanceAnalyzerAgent` and
`FeedbackSynthesizerAgent`, resolving `IGameRunner` and `ITelemetryStore` from
`Nexo.Orchestration.Playtest.Ports`.

That is the **same hardcoded-switch coupling revision 1 itself described for
Tier 3**. Playtest was misfiled. It is Tier 3.

There is also a reference the documented green-check *cannot see*:
`RepoFsToolboxFactory.cs:48` and `:95` do `tools.Register(new TileMapRenderTool())`.
`Nexo.BackgroundAgents.HostRunners` is not a member of `Nexo.Kernel.sln`. Building
that project directly against an applied extraction fails too.

**Tier 1 is empty.** Every candidate has an inbound reference from code that stays.

`scripts/handoff/extract-game-layer.sh` now runs an inbound-reference check first
and **refuses `--apply`** while any blocker stands. Current output:

```
BLOCKED  Nexo.Orchestration.Agents.Playtest  -> AgentFactory.cs, OrchestrationFactoryAndCommunicationTests.cs
BLOCKED  Nexo.Orchestration.Playtest         -> AgentFactory.cs, OrchestrationFactoryAndCommunicationTests.cs
BLOCKED  TileMapRenderTool                   -> RepoFsToolboxFactory.cs, TileMapRenderToolTests.cs, ToolsDevTests.cs
```

The one Tier 1 claim that survived: **`AddPlaytestServices` has zero callers, and
its body is entirely commented out.** It is an empty no-op returning `services`.
Deleting it is safe.

### 2.2 The rename script would have destroyed a git worktree

`rename-to-ashlar.sh` walked the tree with `find`, pruning only `./.git` and
`./_handoff`. On a working checkout that also swept:

| Swept | Files |
|---|---|
| `.claude/worktrees/<name>/` — a parked **git worktree**, 1.5 GB | 4,429 |
| `bin/` + `obj/` build output | 3,896 |
| The actual source tree | 4,192 |
| **Total** | **11,854** |

The worktree is listed in `.git/info/exclude`, so `git status` never shows it and
**`git checkout .` / `git reset --hard` cannot restore it.** Pass 1 also rewrites
that worktree's `.git` gitfile, which holds the absolute path
`.../Nexo-Framework/.git/worktrees/...` — breaking the link permanently.

Fixed by scoping from **`git ls-files`** instead of `find`. Tracked files are
exactly the set that needs renaming; build output regenerates, and a worktree is a
separate checkout that must be renamed on its own branch if at all. The script now
also refuses `--apply` against a dirty tree.

Verified dry run after the fix — matching revision 1's intended figures exactly,
with zero worktree or build-output hits:

```
files with Nexo/NEXO/nexo in content: 4000     # 4005 on this branch, see below
files renamed: 228
directories renamed: 99
```

The filename and directory counts match revision 1 to the file. The content count
is 4,000 measured on `master` and **4,005 on this branch** — the difference is the
five handoff files themselves, which mention `Nexo` throughout. Not a discrepancy;
just remember the number moves with whatever docs you add.

### 2.3 `verify-rename.sh` would have failed a correct rename

Two bugs, both fixed:

1. It scanned with `grep -r .` and `find`, excluding only `.git` and `_handoff`.
   Since the rename is correctly scoped to tracked files, those scans still see
   `Nexo` in build output and in the parked worktree — reporting **FAIL on a
   rename that was entirely correct.** Now scoped to `git ls-files`, with untracked
   residue reported as INFO rather than failure.
2. Its solution-integrity check resolved project paths from the repo root, but
   paths inside a `.sln` are relative to *that solution's* directory. It reported
   3 phantom `MISSING` entries for `application/Nexo.Application.sln` **on a
   completely untouched tree.** Now resolved relative to each solution.

### 2.4 The ordering constraint was unnecessary

Revision 1 insisted extraction must precede the rename, because the rename would
invalidate the extraction script's `Nexo.*` paths. It does not: the extraction
script is a **tracked file**, so pass 1 rewrites its internal paths while pass 3
renames the matching directories. They stay consistent.

Given the extraction is now blocked pending a refactor, **do the rename first.**

---

## 3. Prerequisites

- **.NET SDK 10.x** — `global.json` pins the 10.0 band. Confirmed working here at
  `10.0.400`.
- **Git**, and **bash** (Git Bash is fine — the scripts are POSIX-ish bash).
- No provider credentials. The kernel build and cert gate run offline.

### The blocker you have to clear yourself

`dotnet build Nexo.Kernel.sln` is **green** — 0 warnings, 0 errors, 80 seconds.

`bash scripts/run-cert-gate.sh` **cannot run on this machine**:

```
Catastrophic failure: System.IO.FileLoadException: Could not load file or assembly
'...Nexo.Tests.Infrastructure.dll'. An Application Control policy has blocked this file. (0x800711C7)
```

Zero tests were discovered, zero ran. This is **Windows Application Control
blocking the freshly-built unsigned test assembly** — a machine policy, not a code
defect. Until it is resolved there is *no test signal at all*.

Options: allow the build output path in the Application Control / Smart App
Control settings, or run the gate inside Docker or WSL where the host policy does
not apply, or rely on CI.

**Do not run a 4,000-file rename while the one required CI check cannot execute.**

### A pre-existing failure, unrelated to this work

`dotnet build Nexo.sln` fails at *restore*, on a clean untouched branch:

```
GameDirector.Host.csproj : error NU1201: Project Nexo.API is not compatible with
net8.0. Project Nexo.API supports: net10.0
```

`GameDirector.Host` targets net8.0 but references net10.0-only `Nexo.API`. This
predates the rename work. It matters here only because it means you cannot use
`Nexo.sln` as the broad green-check — build affected projects directly instead.

---

## 4. The sequence

### Step 0 — baseline

```bash
dotnet build Nexo.Kernel.sln     # expect: 0 Warning(s), 0 Error(s)
bash scripts/run-cert-gate.sh    # blocked until you clear §3
```

### Step 1 — the rename

```bash
bash scripts/handoff/rename-to-ashlar.sh            # dry run: expect 4005 / 228 / 99
bash scripts/handoff/rename-to-ashlar.sh --apply
bash scripts/handoff/verify-rename.sh               # must print PASS
dotnet build Ashlar.Kernel.sln
bash scripts/run-cert-gate.sh
```

Run `verify-rename.sh` **before** the build: two of its four checks catch failures
that surface at runtime rather than compile time.

Commit this alone. A 4,000-file diff mixed with anything else is unreviewable.

### Step 2 — the provider refactor (unblocks the extraction)

Introduce the two registries, so `AgentFactory` and `DomainRecognizer` stop
hardcoding game domains:

```csharp
public interface IDomainAgentProvider
{
    bool Handles(string domain);
    BaseAgent Create(AgentSpawnSpec spec);
}

public interface IDomainPatternProvider
{
    IReadOnlyDictionary<string, IReadOnlyList<Regex>> Patterns { get; }
}
```

`AgentFactory` resolves `IEnumerable<IDomainAgentProvider>` and asks each in turn;
`DomainRecognizer` composes its pattern table from the registered providers.
`RepoFsToolboxFactory` needs the same treatment one level down — take its tool set
from a registry instead of newing up concrete tools.

`InfrastructureAgent` and `SecurityAgent` are **not** game-specific. They stay in
the kernel and register as providers like everything else.

Re-run `extract-game-layer.sh` until the inbound-reference check reports `ok` on
all three symbols. That check is the definition of done for this step.

### Step 3 — the extraction

```bash
bash scripts/handoff/extract-game-layer.sh          # must report 0 blockers
bash scripts/handoff/extract-game-layer.sh --apply
```

Then delete `AddPlaytestServices` (empty no-op, zero callers), drop the moved files
from any `.csproj` that lists them explicitly, and verify:

```bash
dotnet build Ashlar.Kernel.sln
dotnet build src/Ashlar.BackgroundAgents.HostRunners/Ashlar.BackgroundAgents.HostRunners.csproj
dotnet build src/Ashlar.Tests.BackgroundAgents/Ashlar.Tests.BackgroundAgents.csproj
```

The last two are **not** in `Ashlar.Kernel.sln` and are exactly where revision 1's
breakage hid.

### Step 4 — make the game layer a repository

`_handoff/game-layer/README.md` has the steps. `GameLayer` is a placeholder — pick
the real name first.

**Nothing is published to nuget.org and there are zero git tags**, so there is no
package to consume yet. Until Wave 6 lands, use a `ProjectReference` into a sibling
checkout, or a local folder feed. `consumer-template/` exists for this.

---

## 5. Tier map (corrected)

| Tier | Contents | Status |
|---|---|---|
| ~~1~~ | — | **empty.** Everything previously here has inbound references. |
| 2 | `Orchestration/Assets/`, `Agents/Assets/` (generative image/audio/3D) | Clean move, but `AgentFactory` constructs these types — cut over with Tier 3. |
| 3 | `Agents/Templates/{Combat,Economy,Gameplay,AI}Agent.cs`, `Architect/DomainRecognizer.cs`, **plus the whole Playtest tree and `TileMapRenderTool`** | A refactor, not a move. Needs the registries in §4 Step 2. |

**Certification fixtures — leave alone.** `DamageResolverSources.cs`,
`HealthApplierSources.cs` and `DamageResolverBrickConstraints.cs` are game-*themed*
but are fixtures for the certification gate, cited by the evidence ledger in
`docs/certification-evidence.md`. Moving them risks the one CI-proven subsystem for
a cosmetic gain. Rename later if the theming bothers you; do not relocate.

---

## 6. Gotchas

| Gotcha | Detail |
|---|---|
| **Parked git worktree** | `.claude/worktrees/` holds a full 1.5 GB checkout, git-excluded and unrecoverable by `reset --hard`. Both scripts now skip it. If you add tooling that walks the tree, skip it there too. |
| **Two clones** | `Downloads/Nexo` and `Downloads/Nexo-Framework` share one origin. `Nexo` has substantial uncommitted work. Do this in `Nexo-Framework`, or commit that work first. |
| **Env var break** | `NEXO_ALLOW_MOCK`, `NEXO_TRUST_ENABLED`, `NEXO_DEPLOYMENT_PROFILE` → `ASHLAR_*`. Shell profiles, `.env` files and CI secrets are untracked — the script cannot reach them. Consider a temporary compat shim that reads the old name and warns. |
| **Config key break** | `Nexo:Security:ExposureProfile` → `Ashlar:Security:ExposureProfile`, and every other `Nexo:*` key. |
| **State directory** | `.nexo/` → `.ashlar/`, matching the brand materials. Existing local state will not be found afterwards. |
| **Container images** | `ghcr.io/ianfrelinger/nexo-cli` → `ashlar-cli`. Does not exist under the new name until published; the audit could not confirm the old one exists either. |
| **Git remote** | The script rewrites the repo URL in docs. It does **not** touch `.git/config`. Rename the GitHub repository separately. |
| **`_handoff/` is skipped** | Both scripts exclude it, so the extracted game layer keeps its own identity. It is untracked anyway, so `git ls-files` excludes it for free. |
| **PublicAPI files** | `PublicAPI.Unshipped.txt` holds fully-qualified type names and is rewritten by pass 1. All entries are `Unshipped`, so no baseline breaks. |

---

## 7. Fix these while you are in there

From the audit. Cheap during this work, genuinely worth doing.

1. **The write-allowlist escape.** `PathAllowlist.Approve` inspects only the `path`
   argument; `RepoFsWriteTool` takes a separate model-supplied `root` and does
   `Path.Combine(args.root, args.path)` with no validation. `root="/etc"` plus
   `path="src/x.cs"` passes every policy and writes outside the repo. Fix by
   **removing `root` from all 14 tool schemas in `src/Nexo.Tools.Dev/`** and
   sourcing it from the sandbox declaration. Add the hostile-root test —
   `Tests/Adaptation/AdversarialScopeEscapeTests` covers hostile `path` only.
   Worth doing **before** the rename: small, independently valuable, and its diff
   stays readable in the vocabulary you already know.
2. **Two lying package descriptions.** `src/Nexo.Lite/Nexo.Lite.csproj:14` and
   `src/Nexo.Sdk/Nexo.Sdk.csproj:14` describe software that does not exist. This is
   package metadata and would ship to a feed verbatim.
3. **Six silent no-ops.** `BehaviorExecutor.cs:443` and `:501`,
   `ImplementationSelector.cs:50`, both pipeline stage adapters, the orchestration
   fallback agent, and `EnableParallel` (`LoopOptions.cs:16` /
   `ParallelLoopKernel.cs:34`). All fail by quietly doing nothing. Make them throw.

---

## 8. Checklist

- [ ] Application Control cleared (or gate moved to Docker/WSL/CI) — **tests can run**
- [ ] Baseline: `dotnet build Nexo.Kernel.sln` green, `run-cert-gate.sh` green
- [ ] Rename applied; `verify-rename.sh` prints **PASS**
- [ ] `dotnet build Ashlar.Kernel.sln` green; cert gate still green
- [ ] Rename committed alone
- [ ] Env vars / config keys / `.env` files updated by hand
- [ ] `IDomainAgentProvider` / `IDomainPatternProvider` / tool registry landed
- [ ] `extract-game-layer.sh` reports **0 blockers**
- [ ] Extraction applied; `AddPlaytestServices` deleted; kernel **and** HostRunners build
- [ ] Extraction committed alone
- [ ] `_handoff/game-layer/` given a real name and moved to its own repo
