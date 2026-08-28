# Hardware bring-up — getting Ashlar running on your own machines

*Written 2026-08-28 against `master` @ `0983d74`.*

**Read this before the closing plan.** `CLOSING-PLAN.md` says what to *build*. This says how to
get what exists onto your hardware today, and how to *verify* each layer so you know which
failures are expected and which are news.

> **Nothing in this document has been executed.** It was written from reading the code, in an
> environment with no .NET SDK, no Docker and no hardware. Commands and paths are real and were
> checked against the tree; the *outcomes* are predictions. Where I expect something to fail, it
> says so and says why — that is the most useful part of this document, and it is also the part
> most likely to be wrong. Trust the machine over this file.

---

## 0. Two probes first — ten seconds, and one of them can invalidate the plan

Run these before anything else. They are the only things that can change the shape of the work.

```bash
# On the Raspberry Pi:
uname -m
```

| Output | Meaning |
|---|---|
| `aarch64` | Good. The published image has a `linux/arm64` manifest. Continue. |
| `armv7l` | **Stop.** You are on 32-bit Raspberry Pi OS. There is no armv7 image and no plan to build one. Either reimage the Pi with 64-bit Raspberry Pi OS, or drop the Pi from the fleet. Nothing downstream works around this. |

```bash
# On the Mac:
echo $DOCKER_DEFAULT_PLATFORM
```

| Output | Meaning |
|---|---|
| empty | Good — Docker picks `linux/arm64` natively on Apple Silicon. |
| `linux/amd64` | The Mac will silently run the **amd64** image under emulation: no error, several times slower, and NSec's native crypto in a configuration nobody has tested. Unset it for Ashlar work, or accept it knowingly. |

Write both answers down. They are inputs to decisions 1 and 5 in `CLOSING-PLAN.md`.

---

## 1. Ground truth before you start

Four things are true today that will otherwise waste an evening. All were verified by direct code
read; the file references are so you can check me.

| What you might expect | What is actually true |
|---|---|
| The daemon runs agents | **It runs zero.** `find application/src/Ashlar.CLI -iname 'appsettings*'` returns nothing, and `BackgroundAgentConfigLoader` binds only `BackgroundAgents:Agents`. With no config the daemon reaches `Task.Delay(Timeout.Infinite)` and sleeps. It will look alive and do nothing. |
| A container keeps its identity | **It does not.** `.docker/Dockerfile.cli` sets `ASHLAR_STATE_DIR` and three siblings, but **not** the key dir or mesh dir, so those land in the container's `HOME`. `docker rm` destroys the node's operator key and every package it published. |
| The installer sets you up as a node | It runs `docker run --rm … background-agent daemon` (`scripts/install/container-bootstrap-linux.sh:248`) — **`--rm`, no state volume**. It is a smoke test, not a deployment. |
| `ashlar keys trust` exists | **It does not.** `KeysCommand` has `init` and `show` only. There is no trust root anywhere yet; that is `CLOSING-PLAN.md` Phase 3. **A node cannot currently refuse a package from an unknown signer.** |

None of these are blockers for the bring-up below. They are the reason the bring-up stops where it
does.

---

## 2. Track A — one machine, from the published image

Fastest path to something real. No source build, no SDK.

```bash
docker pull ghcr.io/ianfrelinger/nexo-cli:latest
docker run --rm ghcr.io/ianfrelinger/nexo-cli:latest --help
```

**Pin the digest.** `:latest` is republished on every master push and its old manifest becomes
garbage-collectable, so a node pinned to `:latest` silently changes under you and a node pinned to
an old `:latest` digest eventually 404s. Take the immutable tag instead:

```bash
docker buildx imagetools inspect ghcr.io/ianfrelinger/nexo-cli:latest    # note the digest + platforms
# then pin: ghcr.io/ianfrelinger/nexo-cli@sha256:<digest>
```

> The `sha-<12>` tags only became multi-arch on master builds as of `0983d74`. **Any `sha-` tag
> published before that is amd64-only** and will fail on the Pi with `no matching manifest for
> linux/arm64`. Use a `sha-` tag from a build after that commit, or the current `:latest` digest.

**Give it a state volume — the installer does not.**

```bash
docker volume create ashlar-state
docker run --rm -v ashlar-state:/data/state ghcr.io/ianfrelinger/nexo-cli:latest keys init
docker run --rm -v ashlar-state:/data/state ghcr.io/ianfrelinger/nexo-cli:latest keys show
```

Run `keys show` twice. **If the fingerprint changes between runs, the volume is not holding the
key** — expected today, because `ASHLAR_KEY_DIR` is unset in the image and the key lands in the
container `HOME` rather than `/data/state`. Workaround until Phase 1:

```bash
docker run --rm -v ashlar-state:/data/state \
  -e ASHLAR_KEY_DIR=/data/state/keys \
  -e ASHLAR_MESH_DIR=/data/state/mesh \
  ghcr.io/ianfrelinger/nexo-cli:latest keys init
```

Those two variables are the whole of Phase 1 step 1. Setting them by hand now tells you whether the
theory holds before anyone edits a Dockerfile.

---

## 3. Track B — from source, if you want to develop

Needs the **.NET 10 SDK** (`global.json` pins `10.0.100`, `rollForward: latestFeature`).

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
dotnet --version          # must satisfy global.json
dotnet build Ashlar.sln   # NOTE: no CI job does this — expect surprises
```

> `dotnet build Ashlar.sln` is **not** run by any automatically-triggered workflow. The four
> workflows that name it are `workflow_dispatch`-only. It may not build. If it fails, that is a
> genuine finding worth recording, not something you did wrong.

Faster inner loop — the kernel only:

```bash
dotnet build Ashlar.Kernel.sln
```

---

## 4. The testing plan

Five layers, each answering one question. **Run them in order** — a failure at L1 makes L3 results
meaningless. Record the result of each; the "if it fails" column tells you whether to stop.

### L0 — Does the artifact run at all?

| # | Command | Expected | If it fails |
|---|---|---|---|
| 0.1 | `uname -m` on each box | `x86_64` or `aarch64` | `armv7l` → that box is out until reimaged |
| 0.2 | `docker run --rm <image> --help` | Verb list incl. `keys`, `pkg`, `verify`, `gates`, `doctor` | On the Pi, a native-load error means NSec's arm64 libsodium does not load. **This has never been executed by anything** — the publish workflow's only smoke test runs on an amd64 runner. A failure here is a real discovery; capture the full stderr. |
| 0.3 | `docker run --rm <image> doctor --json` | JSON, non-fatal exit | Read the JSON before concluding anything; `doctor` reports environment gaps by design |

### L1 — Does state survive?

This is the layer everything else rests on, and the layer most likely to fail today.

| # | Command | Expected | If it fails |
|---|---|---|---|
| 1.1 | `keys init` then `keys show` on a fresh volume | A fingerprint | — |
| 1.2 | `keys show` again in a **new container**, same volume | **The same fingerprint** | Expected to fail without `ASHLAR_KEY_DIR` (§2). Re-run with the env var. If it still changes, the volume mount is wrong — check with `docker run --rm -v ashlar-state:/data/state <image> --help` and `docker volume inspect ashlar-state`. |
| 1.3 | Repeat 1.2 after `docker rm` of the container | Same fingerprint | Same cause and same fix as 1.2 |
| 1.4 | `ashlar init` a project dir, `gates` something, re-run in a new container | The gate record is still there | The gate store lives at `<projectDir>/.ashlar` and has **no environment variable** — it is persisted by where you mount the *workspace*, not by `ASHLAR_STATE_DIR`. Mount the project dir explicitly. |

**Do not proceed to L3 until 1.2 passes.** A node that forgets its identity makes every downstream
result meaningless.

### L2 — Does the repo's own suite pass on your machine?

Needs the SDK (Track B).

| # | Command | Expected | If it fails |
|---|---|---|---|
| 2.1 | `bash scripts/run-cert-gate.sh` | Green. This is the **only required check** on master. | Should be green — it is green in CI on `0983d74`. A local-only failure points at your environment, not the code. |
| 2.2 | `make test-prime-time` | `Category=ProdStyle` across the PrimeTime filter | — |
| 2.3 | `make kernel-coverage-gate` | Green | Slow (~10 min). Coverage-instrumented; be patient before calling a hang. |
| 2.4 | `dotnet test Ashlar.sln` | **Unknown** | Genuinely unmeasured — nothing in CI does this. Whatever it prints is new information. Record the failing project names; several test projects are registered `UNOWNED` in `ci/test-ownership.tsv` precisely because no gate runs them. |

### L3 — Does the product loop work end to end?

```bash
bash scripts/e2e-loop.sh      # from repo root; builds the CLI once, then --no-build
```

This is the repo's own behavioural suite: `init → verify → gates`, real binaries, **a fresh process
per invocation** — which is itself the persistence test. It prints `RESULT <n> <name> PASS|FAIL
<detail>` per scenario and exits non-zero if any fail.

| Expected | If it fails |
|---|---|
| Every `RESULT` line `PASS`, exit 0 | Capture the `RESULT` lines. A `FAIL` here is a product-loop regression and is worth reporting verbatim — this suite is the closest thing the repo has to "does the thing work". |

### L4 — Two machines

**What you can do today**, and what you cannot:

```bash
# Box A — a package leaves the machine
ashlar pkg export ...        # produce the .ashpkg
ashlar pkg publish ...       # write it into the mesh store (a directory)
# move the directory: Syncthing, a mounted share, a USB stick — MeshStore is transport-naive
# Box B
ashlar pkg pull --from <shared-dir>
```

| # | Check | Expected today |
|---|---|---|
| 4.1 | B imports A's package | Should work — the `.ashpkg` envelope is fail-closed and path-allowlisted at both ends |
| 4.2 | B prints A's key fingerprint | **Probably not.** Phase 1 adds the print. |
| 4.3 | **B refuses a package from an unknown third key** | **No. This does not work.** There is no trust root; `ashlar keys trust` does not exist. This is Phase 3, and it is the thing that makes the fleet mean something. |
| 4.4 | Both boxes have distinct identities | Only if you gave each its own `ASHLAR_KEY_DIR`. Note every existing "two-node" scenario in `scripts/e2e-loop.sh` exports **one shared** `ASHLAR_KEY_DIR` and runs both nodes as the same actor. |

**Stop at 4.1/4.2.** Getting a certified package from one machine to another, with both keeping
their own identity across restarts, is a real milestone and is where today's code ends.

---

## 5. Failures that are expected — do not debug these

| Symptom | Why | Fix lives in |
|---|---|---|
| Daemon runs, does nothing, forever | No agent config ships in the image | Phase 1 |
| Key fingerprint changes after `docker rm` | `ASHLAR_KEY_DIR` unset in the image | Phase 1 (workaround in §2) |
| `pkg pull` says `not an ashlar project` | The verb requires `ashlar.yaml` + `ashlar.policy.yaml` in `--path`; a bare node has neither | Phase 1 — `ashlar init` the node's dir |
| `pkg pull` says `the peer store is empty` | Also what it says when the peer is **off** — the two are indistinguishable | Phase 4 |
| No `ashlar` command on the host | There is no host wrapper; the installers print `docker run` examples | Phase 1 |
| Full Platform Readiness Gate red | Was red 20 consecutive runs; root-caused and fixed in `c22916b`. Should now pass — **if it is still red, that is news.** | fixed |
| `/health` returns OK on a broken node | It is a hardcoded literal, by design, today | Phase 1 |

---

## 6. What to record

Keep a file per machine. It is the input to `CLOSING-PLAN.md` Phase 1 and to decisions 1, 2 and 5.

```
box:            <name>
arch:           <uname -m>
os:             <uname -a>
docker:         <docker --version>
image digest:   <sha256:...>
RAM / disk:     <free -h ; df -h>
L0 result:      pass / fail + detail
L1.2 result:    fingerprint stable across containers?  yes / no
L2.1 cert-gate: pass / fail
L3 e2e-loop:    N pass / M fail  + the RESULT lines for any FAIL
key fingerprint: ed25519:...
```

The two most valuable data points for the plan: **whether the arm64 image runs on the Pi at all**
(L0.2 — never executed by anything, ever) and **the Pi's real RAM headroom** under the daemon,
which decides owner decision 12 (per-node inference vs one box serving the LAN).

---

## 7. Where to go next

- `CLOSING-PLAN.md` — the eight phases. Read **Where to stop** first.
- `STATE-2026-08-27.md` — the project-wide audit this all came from.
- `DECISION-identity-split.md` — why there is one operator identity and what it costs.
- `ci/cert-gate-assertions.md` — what the one required check enforces, before you ever consider muting it.

If L0.2 fails on the Pi, say so before doing anything else in the plan. Phases 1 through 5 assume
that image runs.
