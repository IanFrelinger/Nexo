# Moving development onto your own hardware

*Written 2026-08-30 against `master` @ `c210f81`. Nothing in this document has been executed —
see "What is measured" below.*

**This is about moving the *development loop* — build, test, gates — off cloud sessions and onto
your machines.** Its siblings cover different ground, and none of them covers this:

| Document | Answers |
|---|---|
| `HARDWARE-BRINGUP.md` | How do I run the shipped **product** on a machine? |
| `CLOSING-PLAN.md` | What is left to **build**? |
| **this file** | How do I **develop** it here instead of in a cloud session? |

> **What is measured.** Nothing here was run. There is no .NET SDK and no Docker in the authoring
> environment — which is the whole reason this document exists. Every path, target, script and
> line reference below was checked against the tree at `c210f81`; the *outcomes* are predictions.
> Where this document and your machine disagree, your machine is right. Two corrections to other
> docs, made alongside this one, are marked **[fixed]** and were verified against source.

---

## 0. The thing to establish before anything else

**No cloud session in this project has ever compiled this repository.** Every change made from one
was verified by reading code and by CI. That worked — `cert-gate` is a real compiler and it caught
real breakage — but it means one ordinary question has never been answered locally:

> Does this repository build and test on a developer machine at all?

CI answers a *narrower* question than you might assume (§4). So the first milestone on your
hardware is not a feature. It is **D0–D2 in §2**, and until those pass, treat every other result
as provisional.

There is one specific claim worth knowing you are testing: **no automatically-triggered workflow
builds `Ashlar.sln`.** Four workflows name it — `cross-platform-tests.yml`, `perf-certification.yml`,
`test-air-gapped-no-network.yml` are `workflow_dispatch`-only, and `onboarding-docs-guard.yml` only
greps README/docs for the string. So the full-solution build has no automatic gate. It may not
build. That is a discovery, not a prediction, and D2 is where you find out.

---

## 1. Pick a lane

Three ways to get a toolchain. They are not equivalent — pick by what you are doing, not by taste.

| Lane | Command to get started | Use it when |
|---|---|---|
| **A — Dev container in the editor** | VS Code / Cursor → *Dev Containers: Reopen in Container* | Day-to-day work with IntelliSense. Closest to CI. |
| **B — `devbox.sh`** | `bash scripts/handoff/devbox.sh <any command>` | Running gates, one-off builds, anything scriptable. No editor needed. **Start here on Windows.** |
| **C — Native SDK** | `.NET SDK 10.x` **+ the ASP.NET Core 8 runtime** | Only when you cannot use Docker. See the correction below. |

### Lane A — dev container

`.devcontainer/devcontainer.json` pins `mcr.microsoft.com/devcontainers/dotnet:10.0-noble`, adds
docker-outside-of-docker, and caches NuGet in the named volume `ashlar-nuget-packages`. First open
runs `.devcontainer/post-create.sh`, which restores a **five-project subset**, not `Ashlar.sln` —
so a green post-create says nothing about D2.

### Lane B — `devbox.sh`

Runs any command inside the project's dev/test image and is the lane most likely to just work:

```bash
bash scripts/handoff/devbox.sh dotnet build Ashlar.Kernel.sln
bash scripts/handoff/devbox.sh bash scripts/run-cert-gate.sh
bash scripts/handoff/devbox.sh                      # interactive shell
```

It calls `scripts/ensure-devtest-image.sh`, which builds `ashlar-devtest:local` from
`.docker/Dockerfile.devtest` on first use (one runtime download, then Docker layer-caches it) and
is already Git-Bash-aware. It runs as **root** — see the permissions trap in §3.

*Nit, and the only stale thing found in the dev tooling:* `devbox.sh:4`'s own usage example says
`dotnet build Nexo.Kernel.sln`. There is no such file — it is `Ashlar.Kernel.sln`, renamed and the
comment missed. Copy-pasting the documented example fails.

### Lane C — native, and a correction

**[fixed] `CONTRIBUTING.md` told you an SDK-10-only machine was enough. It is not.** It cited
`RollForward=Major` and concluded no separate .NET 8 runtime was needed. That was measured false
while this branch was open: rolling `net8.0` forward onto ASP.NET Core 10 breaks **every
HTTP-hosting test**, because ASP.NET Core 8's `ResponseBodyPipeWriter` predates
`PipeWriter.UnflushedBytes`, which System.Text.Json 10 requires. On the GameDirector `net8.0` suite
in this image: **rolled forward → 10 failed; real 8.0 runtime → 167 passed.**

The failure reads exactly like a product bug, and `cert-gate` never caught it because cert-gate
hosts no HTTP. `.docker/Dockerfile.devtest`, `.devcontainer/post-create.sh` and `devbox.sh` were
all fixed for this; `CONTRIBUTING.md`'s native section was the last place still saying otherwise,
and is corrected in the same commit as this file.

So, natively, install **both**:

```bash
# SDK 10 (pinned by global.json: 10.0.100, rollForward latestFeature)
# AND the ASP.NET Core 8 runtime:
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --runtime aspnetcore
dotnet --list-runtimes | grep 'AspNetCore.App 8\.'   # must print a line
```

Do **not** set `DOTNET_ROLL_FORWARD=LatestMajor`. With 8.0 present, it re-creates the exact bug it
looks like it would fix.

---

## 2. The verification ladder

Ordered cheapest to most expensive. **Stop at the first failure and fix it** — a failure at D1
makes every later result meaningless. Times are order-of-magnitude guesses, not measurements.

### D0 — the toolchain exists · seconds

```bash
dotnet --version                                  # expect 10.0.1xx
dotnet --list-runtimes | grep -E 'App (8|10)\.'   # expect BOTH 8.x and 10.x lines
```

*Fails →* §1 Lane C. A missing 8.x line is the single highest-value thing to fix before continuing.

### D1 — a small slice restores and builds · a few minutes

```bash
dotnet restore Ashlar.LocalDevCore.slnf && dotnet build Ashlar.LocalDevCore.slnf -v minimal
# or: make restore-core && make build-core
```

CLI + domain tests + infra tests, nothing under `commercial/`. *Fails →* the problem is your
toolchain or the NuGet feed, not the repo. Do not proceed.

### D2 — the full solution builds · tens of minutes, and this one is a genuine unknown

```bash
dotnet build Ashlar.sln
```

**Nothing automatic has run this** (see §0). If it fails, that is *news* and worth writing down
precisely — it is not necessarily your machine. `CONTRIBUTING.md` states the intent: `Ashlar.sln`
should build on Linux with a stock SDK and no optional workloads.

### D3 — the required gate passes · minutes

```bash
bash scripts/run-cert-gate.sh
# or: bash scripts/handoff/devbox.sh bash scripts/run-cert-gate.sh
```

This is the **only required status check on `master`**, so it is the bar every PR must clear. It
runs `Ashlar.Tests.Infrastructure` under `-f net8.0`, filtered by `CERT_GATE_FILTER`
(`scripts/cert-gate-config.sh:6`) to three namespaces: `…Tests.Certification`,
`…Tests.Adaptation.GenerationSafety`, and `AstMutationEngineTests`. Expect roughly **178 tests** —
there is deliberately no hardcoded expected count; a zero-test guard fails loudly if discovery
returns nothing.

### D4 — the product loop works end to end · minutes

```bash
bash scripts/e2e-loop.sh
```

Real binaries, `init → verify → gates`, a fresh process per invocation — so anything that survives
between lines genuinely survived process death. Emits `RESULT` lines; non-zero exit if any
scenario fails.

### D5 — the wide suites · long, and expect some red

```bash
make test-prime-time        # Category=ProdStyle across Ashlar.PrimeTime.slnf (8 test assemblies)
make test                   # ASHLAR_ALLOW_MOCK=1 dotnet test Ashlar.sln
make kernel-coverage-gate   # before PRs touching Core.Domain / Core.Application / Infrastructure
make testing-strategy-gate  # before opening any PR; diffs against origin/master
```

`make test` runs the whole solution, which is far more than any PR check runs (§4). **Treat first
red here as information, not as a regression** — some of it will be tests that no gate has ever
executed. Record what fails before fixing anything; the list itself is the useful artifact.

---

## 3. Traps that were measured

Each of these cost someone real time already.

**Git Bash rewrites paths.** POSIX-looking arguments become Windows paths before `docker.exe` sees
them, so `-e ASHLAR_KEY_DIR=/data/state/keys` arrives as `C:\…` and dies on
`Access to the path '/app/C:' is denied`. Looks like a product bug; is not.

```bash
export MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*'
```

With conversion off, `docker build` / `compose -f` then need **Windows-style** paths. Both
directions bite. `devbox.sh` and `ensure-devtest-image.sh` already handle this internally; ad-hoc
`docker run` lines do not. On Windows, `wsl --install -d Ubuntu` sidesteps the whole class —
Docker Desktop's own `docker-desktop` distro is a utility VM, not a usable shell.

**The container runs as root, and one test premise cannot hold there.** Any test asserting "this
path is not writable" is meaningless under root. `TileMapRenderTool_reports_render_error_when_output_directory_not_writable`
is the known one; it now probes and skips when privileged. If you add permission-sensitive tests,
guard them the same way — CI is non-root, so they still run where they mean something.

**Roll-forward.** Covered in §1 Lane C. Repeated here because it is the one that reads as a product
bug: 10 failures that are really a missing runtime.

**NuGet caching is per-lane.** Lane A uses volume `ashlar-nuget-packages`; `devbox.sh` uses
`nexo-nuget-packages-root` (still the old name). Different volumes, so a first restore in each lane
pays full freight. Not a bug, just a surprise on the clock.

---

## 4. What "green locally" does and does not buy you

Worth internalising before you trust a green run, and the main reason failing tests accumulated here
in the first place.

- **`cert-gate` is the only required check on `master`.** Everything else is advisory.
- It runs **~178 tests**. The repo contains roughly **4,315** `[Fact]`/`[Theory]` methods. So the
  required gate exercises about **4%** of the suite.
- **`layer-boundary / verify` is not required.** A PR touching `application/src/Ashlar.API` or
  `Ashlar.CLI` without an exemption merges with that check red. That is a known, documented gap —
  read a red `verify` before merging rather than assuming it is noise.
- **`ci/test-ownership.tsv` is the ledger of what no PR check runs.** Three projects are `UNOWNED`
  with a `2027-03-31` expiry and one with `2027-06-30`. `TestOwnershipConventionTests` runs inside
  cert-gate, so a new test project that is not registered there **cannot merge** — and a *passed*
  expiry date blocks every PR in the repo with no code change. Read the header before touching a
  date; the first version of that file was a scheduled outage and was re-dated before it tripped.

The practical consequence: **local D5 is a stronger signal than a green PR.** If you want the
accumulation to stop, D5 on your machine is the thing that actually catches it.

---

## 5. If you want the agent convergence loop locally

`scripts/readiness-gate-local.sh` is the objective function — per layer, it runs what the
layer-owning CI workflows run, plus strictly stronger local coverage:

```bash
bash scripts/readiness-gate-local.sh --layer application --json /tmp/gate.json
```

Layers are `application`, `applications`, `apps`. `--include-tier-d` adds the Docker-dependent
lanes. It deliberately does not use `-e`: every gate runs, failures are counted rather than fatal.

For the full container-first pipeline, `scripts/readiness-container-setup.sh` provisions it — run
**inside** the container. Three of its defaults are stale and will point you at the wrong thing:

| Variable | Default | Reality |
|---|---|---|
| `READINESS_SRC` | `/workspaces/Nexo` | Correct only if your checkout folder is literally `Nexo` |
| `READINESS_CLONE` | `/workspaces/nexo-agent` | Fine, but note it is a *separate clone* — integration commits land there, not in your working tree |
| `READINESS_BRANCH` | `claude/recursing-franklin-cbb828` | **Long merged.** Override it. |

```bash
READINESS_SRC=/workspaces/<your-folder> READINESS_BRANCH=<your-branch> \
  bash scripts/readiness-container-setup.sh
```

Background: `_handoff/readiness/README.md` and `HANDOFF.md`.

---

## 6. State of play

`master` @ `c210f81`. Since the bring-up guide was first written, thirteen commits landed, and the
plan was **retargeted at the hardware that exists — a Windows box and an M-series MacBook, not a
Raspberry Pi** (#410). An M-series Mac exercises the same `linux-arm64` RID a Pi would, so that
made the oldest unknown cheaper rather than harder.

**Landed:** Phase 1 steps 1–2 (#415), 4 and 7 (#418), 10 (#417); Phase 5 steps 1 (#414) and 6
(#416); the container runtime fix (#411); two intermittent test fixes (#412, #413).

**Phase 1 still open:** steps 3, 5, 6, 8, 9, 11, 12, 13 — the entrypoint script, park-don't-exit,
the heartbeat and `HEALTHCHECK`, the installer host wrapper, the update scripts, and three
convention tests. Phase 2 (the write floor) has not started; its `ForgeApplier` normalization bug
is the one item in the plan that bypasses every other control.

**[fixed] `HARDWARE-BRINGUP.md` was one commit stale.** It said "the operator is not shown which key
signed a held package … and no fingerprint", citing it as the real gap. #417 landed exactly that —
`PkgCommand.cs:460` now appends `· sealed by {Fp(signer)}` on every branch with a verified sealer.
Corrected in the same commit as this file. The *narrower* gap it names is still real: a node cannot
auto-admit Alice while refusing Bob, because there is no trust root and `ashlar keys trust` does
not exist. That is Phase 3.

---

## 7. Record what your machines actually do

The value of this document is the delta between it and reality. Fill one in per machine and put it
back here — `HARDWARE-BRINGUP.md` §0 was rewritten from exactly this kind of record, and four of its
claims turned out to be wrong.

```
MACHINE:        (windows-desktop | macbook-m?)
ARCH:           uname -m →
DOCKER:         docker version →
DOCKER_DEFAULT_PLATFORM:   (must be unset on the Mac)
LANE:           A (devcontainer) | B (devbox.sh) | C (native)
SDK:            dotnet --version →
RUNTIMES:       dotnet --list-runtimes | grep -E 'App (8|10)\.' →

D0 toolchain            PASS / FAIL —
D1 LocalDevCore build   PASS / FAIL —          (minutes:)
D2 Ashlar.sln build     PASS / FAIL —          (minutes:)   ← genuinely unknown
D3 cert-gate            PASS / FAIL — tests discovered:
D4 e2e-loop.sh          PASS / FAIL —
D5 make test            PASS / FAIL — failures: (attach the list)

SURPRISES / things this document got wrong:
```
