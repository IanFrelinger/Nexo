# Tester quickstart

One lane, one command sequence, about fifteen minutes. You will build Nexo from source, run the API on loopback, submit one task and read its output **and** the audit trail it left, then watch the certification gate admit correct code and reject buggy code. No API keys, no model server, no Docker.

Every command below was checked against the code and run from this checkout on Windows 11 with .NET SDK 9.0.317 before this page was written; the same commands work in bash on Linux/macOS. If a command on this page does not do what it says, that is a bug worth reporting (section 5).

## 0. Prerequisites

- Git and the **.NET SDK 9.x** (`global.json` pins the 9.0 band; `dotnet --version` should print `9.0.x`). The hosts target `net8.0` and roll forward onto the 9.x runtime (`RollForward=Major`, set in `Directory.Build.targets`), so you do **not** need a separate .NET 8 runtime.
- **Docker is optional.** Nothing on this page needs it. It is required only for the experimental autonomy loop (section 6), which builds and runs model-proposed code inside attested containers.
- No provider credentials. The walk-through uses the mock provider, which the runtime refuses to use unless you set `NEXO_ALLOW_MOCK=1` explicitly (a fail-closed default; see `src/Nexo.Infrastructure/Execution/ProviderFactory.cs`).

## 1. Clone and build the kernel

```bash
git clone https://github.com/IanFrelinger/Nexo.git && cd Nexo
dotnet build Nexo.Kernel.sln
```

`Nexo.Kernel.sln` is the kernel spine plus its test projects; it also builds `Nexo.API`, because `src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj` hosts the API in-process. Expect a few minutes cold. `Nexo.CLI` is not in this solution and builds on its first `dotnet run` in the next step.

## 2. Ask the doctor

Run from the repository root (the doctor's CLI smoke check re-invokes `dotnet run --project application/src/Nexo.CLI -- --help`, so the relative path must resolve):

```bash
dotnet run --project application/src/Nexo.CLI -- doctor
```

Look for the `overall: PASS` line (it is followed by a few "recommended next steps"). `container smoke: warn` is normal when Docker is absent; the container lane is optional. Add `--json` for machine-readable output.

Known trap: `doctor` probes `docker info` with no timeout (`application/src/Nexo.CLI/Commands/BootstrapRuntime.cs`, the `docker` dependency probe). If Docker Desktop is installed but its daemon is wedged, `doctor` hangs at that probe; quit Docker Desktop (or run with the Docker CLI off `PATH`) and re-run.

## 3. The hero: submit a task, read the audit trail

Start the API in one terminal. It listens on `http://localhost:5000` (loopback), **HTTP only, no authentication** - the shipped defaults are `Nexo:Security:ExposureProfile=Localhost` and `AuthorizationMode=None` (`application/src/Nexo.API/appsettings.json`). The exposure rule is fail-closed: declaring `Lan`, `Tailnet` or `Public` without a built-in auth mode makes the API **refuse to start** (`application/src/Nexo.API/Program.cs`, "Security: exposure profile"). Do not put this process on a network as-is; see `SECURITY.md`.

```bash
# bash
NEXO_ALLOW_MOCK=1 dotnet run --project application/src/Nexo.API
```

```powershell
# PowerShell
$env:NEXO_ALLOW_MOCK = '1'
dotnet run --project application/src/Nexo.API
```

Wait for `Now listening on: http://localhost:5000`. In a second terminal:

```bash
curl -s http://localhost:5000/health
# {"status":"healthy","timestamp":"..."}

curl -s http://localhost:5000/api/copilot/task \
  -H "Content-Type: application/json" \
  -d '{"task": "Summarize what this repository does", "auditCount": 5}'
```

```powershell
Invoke-RestMethod http://localhost:5000/health

$r = Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/copilot/task `
  -ContentType 'application/json' `
  -Body (@{ task = 'Summarize what this repository does'; auditCount = 5 } | ConvertTo-Json)
$r | Select-Object taskId, success, summary
```

The route is `POST /api/copilot/task` (`application/src/Nexo.API/Endpoints/NexoEndpoints.cs`); the body is `CopilotTaskRequest(string Task, int AuditCount = 25)` (`application/src/Nexo.API/Endpoints/CopilotTaskRequest.cs`). The response is `CopilotTaskResponse`: `taskId`, `tenantId` (`default` unless you send `X-Nexo-Tenant`), `success`, `summary` (for example `1 agent(s) executed`), `output` (per-agent results), `isTrustPaused`, and `recentAudit`. Two things to know when reading it:

- With the mock provider the `output` text is deterministic scaffolding (a "fallback decomposition"), not model output. The point of this page is the **record**, not the prose. To route to a real model, set `NEXO_OLLAMA_BASE_URL` / `OPENAI_API_KEY` instead of `NEXO_ALLOW_MOCK` (see `docs/Configuration.md`).
- `recentAudit` holds the entries recorded **before** this task (the handler snapshots the log, then records the task), so it is `[]` on your first call and shows the previous task on the second.

Now read the trail the task left:

```bash
TASK_ID=<taskId from the response>
curl -s http://localhost:5000/api/copilot/tasks/$TASK_ID     # the stored record: task, submittedAt, completedAt, success, summary
curl -s http://localhost:5000/api/copilot/tasks              # history, newest first
curl -s http://localhost:5000/api/trust/dashboard            # recentAudit: eventType=CopilotTask, disposition=Success, sourceId=<taskId>
curl -s http://localhost:5000/api/activity/feed              # the same event as an activity entry
curl -s http://localhost:5000/api/trust/status               # isPaused, active policy pack
```

```powershell
Invoke-RestMethod "http://localhost:5000/api/copilot/tasks/$($r.taskId)"
(Invoke-RestMethod http://localhost:5000/api/trust/dashboard).recentAudit |
  Select-Object eventType, disposition, sourceId
```

That is the whole claim in one screen: the task ran, its record is stored under a task id, and the trust log carries a `CopilotTask` entry whose `sourceId` is that id. The portal at `http://localhost:5000` and Swagger at `http://localhost:5000/swagger` show the same data. Stop the API with Ctrl+C when done.

## 4. See it certify

The certification gate is what makes "certified" a checkable claim rather than a label: analyzer fence, then witness (correctness), then mutation testing (does the witness have teeth), then determinism. It runs in CI as `cert-gate`; reproduce it locally with the same filter (`scripts/cert-gate-config.sh` is the single source of truth for that filter):

```bash
bash scripts/run-cert-gate.sh
```

If you want the smallest slice (the two gate-teeth classes, 16 tests, under two minutes on a laptop), run them directly:

```bash
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 \
  --filter "FullyQualifiedName~CertificationGateTeethTests"
```

Watch for the pair that defines the gate: `GoodBrick_StrongWitness_Admits_WithZeroEscapeRate` (ADMIT, `escape_rate=0`) and `WeakWitness_AllowsMutantEscapes_RejectsWithTeeth` (REJECT on `mutation`). The ledger row for each, with the CI run that proved it, is in `docs/certification-evidence.md`.

To author something the gate can judge, start from the reference brick: `samples/hello-brick/README.md` (`dotnet test samples/hello-brick/HelloBrick.Tests/HelloBrick.Tests.csproj`), then `docs/AuthoringBricks.md`. Nothing is on nuget.org yet, so the sample uses a `ProjectReference` into `src/`.

## 5. What to test, what to report

Worth your time:

- **Time to first audited job.** Fresh clone to a `CopilotTask` entry in `/api/trust/dashboard`. Note the wall-clock and every place you had to guess.
- **Does the doctor tell the truth?** Break something (rename `dotnet`, unset the SDK) and see whether `doctor` names it.
- **Does the API fail closed?** Set `Nexo__Security__ExposureProfile=Lan` with no auth mode and confirm the refusal to start; then set `Nexo__Security__AuthorizationMode=ApiKey` and `Nexo__Security__ApiKey=...` and confirm `POST /api/copilot/task` returns 401 without the `X-Nexo-Api-Key` header.
- **Does the gate have teeth?** Weaken a witness in `src/Nexo.Tests.Infrastructure/Tests/Certification/` and confirm the mutation leg rejects.
- **Do these docs match the code?** Every path and command on this page is meant to be exact.

Report through the issue templates: `.github/ISSUE_TEMPLATE/bug_report.md`, `.github/ISSUE_TEMPLATE/feature_request.md`, `.github/ISSUE_TEMPLATE/integrator_feedback.md`. Include OS, `dotnet --version`, the exact command, and the output. Security findings go through `SECURITY.md`, not a public issue.

## 6. Known limitations (read before judging)

- The certification gate's own limits are listed under **Known v0 limitations** in `docs/certification-evidence.md`: a development HMAC signer rather than PKI, a type-level (not semantic) composition seam check, runtime-derived expected test counts, and the exact boundary of session containment.
- The **autonomy loop is experimental and ships in hold mode**: it certifies fully and admits nothing (`HoldAdmission=true` by default, `src/Nexo.Infrastructure/Autonomy/NexoAutonomyOptions.cs`). It needs a container engine and a local model server, and its evidence so far is spike-grade (`docs/certification-evidence.md`, rows P2 through S5). Start from `samples/autonomy-objectives/README.md`; the flight runner `spikes/autonomy-first-flight/run-first-flight.ps1` is a spike, not a supported entry point (`spikes/README.md`).
- Local defaults are HTTP-only with no auth. That is intentional for this page and wrong for any network.
- No packages are on nuget.org yet; consume by `ProjectReference` (`samples/hello-brick/`) or from a feed you supply (`consumer-template/CONSUMING.md`).
- The mock provider proves the plumbing and the record, not model quality.

Next: `docs/GettingStarted.md` for the pipeline and CLI tour, `docs/CopilotMvpWalkthrough.md` for the portal and trust-control commands, `docs/IntegratorGuide.md` to embed Nexo in your own host, `docs/DocsIndex.md` for everything else.
