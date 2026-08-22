# Handoff — game-layer extraction and the Ashlar rename

Prepared on branch `claude/nexo-explanation-q4xnbr` in a cloud session that had **no
.NET SDK**, so nothing below has been compiled or tested. Everything here is either
static analysis of the source or a script that has been dry-run but not applied.
That is the whole reason for the handoff: the remaining work needs `dotnet build`
in the loop.

---

## 1. Where things stand

Two decisions were made in the originating session:

| Decision | Answer |
|---|---|
| **Rename scope** | **Full rename.** `Nexo` → `Ashlar` across namespaces, assemblies, packages, directories and filenames. |
| **Game code destination** | **Its own repository**, consuming the kernel as a package. |

Nothing has been applied. The working tree is unchanged apart from the three scripts
and two documents added under `scripts/handoff/` and `docs/handoff/`.

### Reference material produced in that session

- **The Nexo Dossier** — full source map, usage modes, competitive positioning, six
  pitches, and an audited list of where documentation and code disagree.
  <https://claude.ai/code/artifact/a1b2328c-05ac-4fad-b7c5-283ee07ebf06>
- **Nexo Cleanup Plan** — seven waves, sequenced, with file-level work items.
  <https://claude.ai/code/artifact/bcfebaf4-6b03-42df-8e8d-0ca2c6424689>

Both were written pre-rename and still say "Nexo" throughout. Re-point them after
the rename lands.

---

## 2. What you need installed locally

- **.NET SDK 10.x** — `global.json` pins the 10.0 band; `dotnet --version` should
  print `10.0.x`. Libraries multi-target `net8.0;net10.0` and executables roll
  forward, so no other runtime is required.
- **Git**, and **bash** (Git Bash is fine on Windows — the scripts are POSIX-ish
  bash and use `find -print0`).
- **Docker** — optional. Only the autonomy spike and the container lanes need it.
- No provider credentials. The certification gate and kernel tests run offline.

Get the branch:

```bash
git fetch origin claude/nexo-explanation-q4xnbr
git checkout claude/nexo-explanation-q4xnbr
```

Establish a green baseline **before changing anything** — you need to know which
failures you caused:

```bash
dotnet build Nexo.Kernel.sln
bash scripts/run-cert-gate.sh          # the one required CI check
```

---

## 3. The sequence

Order matters. The extraction uses `Nexo.*` paths; running the rename first
invalidates every one of them.

### Step 1 — Extract the game layer (Tier 1)

```bash
bash scripts/handoff/extract-game-layer.sh            # review the plan
bash scripts/handoff/extract-game-layer.sh --apply
```

Moves 27 files into `_handoff/game-layer/`. Then, by hand:

1. Delete `AddPlaytestServices` from `src/Nexo.Orchestration/ServiceCollectionExtensions.cs`
   (around line 157). **It has zero callers anywhere in the repository** — verified
   by grep across `src/`, `application/`, `commercial/` and `applications/` — so
   this is a pure deletion.
2. If `Nexo.Orchestration.csproj` or `Nexo.Tools.Dev.csproj` list the moved files
   explicitly (rather than globbing), drop those entries.
3. `dotnet build Nexo.Kernel.sln` — **must still be green.** If it is not, the
   extraction took something the kernel needed and the script's Tier boundaries
   were wrong; revert and report which type broke.

Commit this on its own.

### Step 2 — The rename

```bash
bash scripts/handoff/rename-to-ashlar.sh            # dry run, reports counts
bash scripts/handoff/rename-to-ashlar.sh --apply
bash scripts/handoff/verify-rename.sh               # must print PASS
dotnet build Ashlar.Kernel.sln
```

Dry-run figures from this tree: **4,003 files** with token content, **228 file
names**, **99 directories**.

`verify-rename.sh` checks four things and is worth running before the build,
because two of them fail at runtime rather than compile time:

1. no residual `Nexo` / `NEXO` / `nexo` in file content
2. no residual `nexo` in any path
3. every `.csproj` path named by a `.sln` / `.slnf` resolves
4. every `ProjectReference` resolves

Commit this on its own too. A 4,000-file diff mixed with anything else is
unreviewable.

### Step 3 — Make the game layer a repository

`_handoff/game-layer/README.md` (written by the extraction script) has the steps.
Summary: pick the real name — `GameLayer` is a placeholder — then `git init`, copy
`nuget.config` and `Directory.Packages.props` from `consumer-template/`, and
reference the kernel.

**Caveat you already know about:** nothing is published to nuget.org, and there are
zero git tags, so there is no package to consume yet. Until Wave 6 of the cleanup
plan lands, use a `ProjectReference` into a sibling Ashlar checkout, or a local
folder feed packed from one. The scripts under `scripts/verify-external-product-shape.sh`
build such a feed.

---

## 4. What was deliberately left undone

The extraction script moves **Tier 1 only** — code with no inbound references from
kernel production code. Two tiers remain, and both need a compiler:

### Tier 2 — asset generation *(clean move, wrong time)*

`src/Nexo.Orchestration/Assets/` and `src/Nexo.Orchestration/Agents/Assets/`
(generative image / audio / 3D). Nothing outside `Nexo.Orchestration` references
them, so the move itself is clean — but `AgentFactory` constructs these types, so
they have to cut over at the same moment as Tier 3. Move them together.

### Tier 3 — domain agents and the recognizer *(a refactor, not a move)*

`Agents/Templates/{Combat,Economy,Gameplay,AI}Agent.cs` and
`Architect/DomainRecognizer.cs`.

`AgentFactory.CreateAgent` switches on hardcoded domain strings (`"combat"`,
`"economy"`, `"ai"`, `"gameplay"`) and news up the concrete type.
`DomainRecognizer` hardcodes regex patterns for the same domains plus ammo, loot,
NPCs and pathfinding. Extracting them means turning both into registries the game
package fills:

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

`AgentFactory` then resolves `IEnumerable<IDomainAgentProvider>` and asks each in
turn; `DomainRecognizer` composes its table from the registered providers.

Note that `InfrastructureAgent` and `SecurityAgent` in the same folder are **not**
game-specific. They stay in the kernel and register as providers like everything
else.

Roughly a day of work, and the only part of the extraction that can break the
kernel.

### Certification fixtures — recommend leaving alone

`DamageResolverSources.cs`, `HealthApplierSources.cs` and
`DamageResolverBrickConstraints.cs` live in `src/Nexo.Infrastructure/Adaptation/Generation/`
and are game-themed, but they are **fixtures for the certification gate**, not game
features. The evidence ledger in `docs/certification-evidence.md` cites the tests
that consume them. Moving them puts the one genuinely CI-proven subsystem at risk
for a cosmetic gain. Rename them later if the theming bothers you; do not relocate
them during this work.

---

## 5. Known gotchas

| Gotcha | Detail |
|---|---|
| **Env var break** | `NEXO_ALLOW_MOCK`, `NEXO_TRUST_ENABLED`, `NEXO_DEPLOYMENT_PROFILE` and friends all become `ASHLAR_*`. Anything in your shell profile, `.env` files, or CI secrets needs updating. Consider a temporary compat shim that reads the old name and warns. |
| **Config key break** | `Nexo:Security:ExposureProfile` → `Ashlar:Security:ExposureProfile`. Same for every `Nexo:*` key in `appsettings.json` and compose files. |
| **State directory** | `.nexo/` → `.ashlar/`, which matches the brand materials (`.ashlar/course.json`). Existing local state will not be found after the rename. |
| **Container images** | `ghcr.io/ianfrelinger/nexo-cli` becomes `ghcr.io/ianfrelinger/ashlar-cli`. The image does not exist under the new name until you publish it. Verify the old one even exists first — the audit could not confirm it. |
| **Git remote** | The script rewrites the repo URL in docs. It does **not** touch `.git/config`. Rename the GitHub repository separately, or leave the remote pointing at `Nexo` and fix the docs afterwards. |
| **`_handoff/` is skipped** | Both the rename script and the verifier exclude it, so the extracted game layer keeps its own identity rather than becoming `Ashlar`. |
| **PublicAPI files** | `PublicAPI.Unshipped.txt` holds fully-qualified type names and is rewritten by the content pass. They are all `Unshipped` (nothing is frozen), so no baseline breaks. |

---

## 6. Fix these while you are in there

Three findings from the audit that are cheap to fix during this work and are
genuinely worth doing. Full detail in the cleanup plan artifact.

1. **The write-allowlist escape.** `PathAllowlist.Approve` inspects only the `path`
   argument; `RepoFsWriteTool` takes a separate model-supplied `root` and does
   `Path.Combine(args.root, args.path)` with no validation. `root="/etc"` plus
   `path="src/x.cs"` passes every policy and writes outside the repo. Fix by
   **removing `root` from all 14 tool schemas in `src/Nexo.Tools.Dev/`** and
   sourcing it from the sandbox declaration instead. Add the hostile-root test —
   `Tests/Adaptation/AdversarialScopeEscapeTests` covers hostile `path` only.
2. **Two lying package descriptions.** `src/Nexo.Lite/Nexo.Lite.csproj:14` and
   `src/Nexo.Sdk/Nexo.Sdk.csproj:14` describe software that does not exist. These
   are package metadata and would ship to a feed verbatim.
3. **Six silent no-ops.** `BehaviorExecutor.cs:443` and `:501`,
   `ImplementationSelector.cs:50`, both pipeline stage adapters, the orchestration
   fallback agent, and `EnableParallel` (`LoopOptions.cs:16` /
   `ParallelLoopKernel.cs:34`). All fail by quietly doing nothing. Make them throw.

---

## 7. Verification checklist

- [ ] Baseline green before any change (`dotnet build Nexo.Kernel.sln`, `run-cert-gate.sh`)
- [ ] Extraction applied; `AddPlaytestServices` deleted; kernel still builds
- [ ] Extraction committed alone
- [ ] Rename applied; `verify-rename.sh` prints **PASS**
- [ ] `dotnet build Ashlar.Kernel.sln` green
- [ ] `bash scripts/run-cert-gate.sh` still green — this is the only required CI check
- [ ] Rename committed alone
- [ ] Env vars / config keys / `.env` files updated in your local setup
- [ ] `_handoff/game-layer/` given a real name and moved to its own repo
