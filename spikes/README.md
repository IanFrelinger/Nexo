# Spikes

Everything under `spikes/` is **evidence, not product**. A spike is a script plus the artifacts it produced, kept in the tree so that a row in the certification ledger (`docs/certification-evidence.md`) can point at exactly what was run and what came out. Spikes are cited as PASS/REJECT evidence in that ledger; they are **not a supported entry point** — no CI gate runs them, no compatibility promise covers their flags or output, and they may assume a specific host (Docker socket, a local Ollama, a Windows PowerShell 5.1 shell). Start from `docs/TesterQuickstart.md` for a supported first run.

Two spikes are tracked (27 files besides this README).

## `spikes/autonomy-first-flight/` — one real iteration of the autonomy loop

| File | What it is |
|------|------------|
| `run-first-flight.ps1` | The flight runner. Builds and runs the flight binary **inside the repo's devcontainer image** (Windows Smart App Control blocks fresh unsigned DLLs on the host, and only committed state flies — it clones from a read-only mirror of `-Ref`, default `HEAD`), talking to the **host** Docker daemon through the mounted socket, so the attested session containers the flight starts are siblings on the host daemon. Modes: `-Dry` (TestKit fake session runner, wiring proof only), default (live daemon, one iteration through the full gate then a Tier-0 hot swap and watch window), `-SessionBuild` (candidate compiles inside the attested `dotnet/sdk:9.0` session), `-SessionExecute` (witness, determinism and every mutant execute inside the session), `-Proposed` (recorded model proposal from `FirstFlight/RecordedProposal.cs`), `-Live` (calls Ollama at flight time; witness-blind prompt in `live-proposal-prompt.md`; recording committed under `recordings/`), `-Sweep` (one sweep of the standing loop over the objective store), `-SweepLive` with `-Models`, `-MaxObjectives`, `-MaxTokens`, `-TimeoutMinutes`, `-ThinkOff` (the dogfood-campaign mode: one campaign per model, hold admission on, everything recorded under a host-mounted campaign directory). |
| `FirstFlight/FirstFlight.csproj`, `Program.cs` | The flight binary: composes the autonomy host, runs one iteration, prints the verdict and the certificate inputs. |
| `FirstFlight/LiveProposal.cs`, `RecordedProposal.cs`, `SweepMode.cs`, `FlightLogScanner.cs` | The live-proposer call, the recorded-proposal replay, the sweep/campaign driver, and `FlightLogScannerBrick` — the flight's scratch brick (the same deterministic error-count shape the gate-teeth suites certify with zero escapes; its instance and its source string must stay semantically identical because the witness executes one and the mutation gate compiles the other). |
| `live-proposal-prompt.md` | The exact prompt sent to the model in `-Live` mode. It carries the objective's declarations and deliberately not its witness. |
| `recordings/live-2026*.json` | Four committed live-proposal recordings (2026-08-14): the P6 samples the ledger judges. |

Ledger rows that cite this spike (all in `docs/certification-evidence.md`): **Autonomy first flight (live engine)** — PASS `AdmittedAndSwapped`, `escape_rate=0` @ `1afac86d`; **P3** in-session build @ `d71d045f`; **P5a** in-session execution and **P5b** model-proposed candidate @ `bf8821db`; **P6** LIVE model proposal (PASS on sample 4, acceptance 1/4) @ `4ad4d05e`; **S1** first standing-loop sweep (REJECT at `correctness`) @ `061c4f83`; **S2** repair loop to ADMIT → `CertifiedButHeld` @ `7cdf9e88`; **S3** repair channel as policy; **S4** dogfood campaign 1 (`.ashlar/campaign/*` recordings, `samples/autonomy-objectives/door-lock-transition.proposal.json`); **S5** dogfood campaign 2 across three proposer models — including the equivalent-mutant gate soundness finding on `semver-parse`. Sections "Autonomy first flight (P2)" through "S5" narrate each run.

Prerequisites if you want to re-fly it: Docker with the socket mountable, the devcontainer image buildable, and for `-Live` / `-SweepLive` a reachable Ollama with the named model pulled. Expect it to take a while and to be sensitive to the host; read the section of the ledger for the mode you are running first, and keep `HoldAdmission` on.

## `spikes/portability/` — atom portability, steps 1–5

| File | What it is |
|------|------------|
| `run-portability-spike.sh` | Orchestrates the five steps: (1) generate a deterministic probe brick (`ErrorSummaryExtractor`, a log scanner) via `INewBrickGenerator`; (2) certify it through the S0–S2 gate to a signed admission record; (3) pack `Ashlar.Brick.Contracts` + `Ashlar.Authoring` (+ `Hosting.Bundle`) at the `VERSION` pin into a local feed; (4) consume the generated brick from an external project template using package pins only; (5) assert a cross-project HTTP execute. Writes `spike-run-summary.md` (untracked). |
| `generate-probe-brick.sh`, `certify-probe-brick.sh`, `pack-local-feed.sh` | The individual steps, callable on their own. |
| `tools/GenerateProbeBrick/` | The generator host used by step 1. |
| `witness/error-summary-extractor.witness.json`, `witness/error-summary-extractor.weak.witness.json` | The strong witness that admits and the deliberately weak witness the mutation leg must reject. |
| `generated/ErrorSummaryExtractorBrick/`, `generated/manifest.json`, `generated/error-summary-extractor.json`, `generated/certification-record.json` | The generated brick, its manifest, and the signed admission record (`escape_rate=0`) that step 2 produced. |
| `templates/ExternalProductProbeClient.cs` | The external-consumer client used by steps 4–5. |
| `REPORT.md` | Pointer only — the evidence ledger moved to `docs/certification-evidence.md`. |

Ledger rows that cite this spike: **Atom portability (spike steps 1–5)** — PASS on all steps (proof index and the "Atom portability (spike)" section), and the "Artifacts" section, which lists the three `generated/` files above.

## Rules for adding a spike

- One directory per spike, a runner at its root, and a row in `docs/certification-evidence.md` that names the runner and the commit it was flown at. A spike nobody cites is deleted.
- Commit the artifacts the ledger row depends on (recordings, certification records, witnesses); leave run summaries and campaign directories untracked.
- Say in the runner header what host it assumes. When a spike is linked from `README.md`, `docs/GettingStarted.md`, or `docs/TesterQuickstart.md`, the link must say it is a spike and point at this file for the caveats and at the ledger row for the result.
