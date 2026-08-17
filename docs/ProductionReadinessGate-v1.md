# Production Readiness Gate v1

This document defines a strict, repeatable gate for deciding whether Nexo is ready for production deployment in a given environment.

The gate is intentionally binary:

- **PASS**: all required checks pass, no unresolved High/Critical exceptions.
- **FAIL**: any required check fails, or any High/Critical exception remains unresolved.

---

## 1) Scope and intent

This gate covers:

- Build integrity and framework compatibility.
- Pipeline runtime correctness (validation, scheduling, fallback, retries, resume, durable persistence).
- Host wiring and CLI operability for production-style workflows.
- Minimal operational evidence from logs and JSON output.

This gate does **not** replace:

- Formal threat modeling.
- Full load/performance certification.
- Regulatory/compliance audits.

Those should run as additional gates (v2+). For a **structured program** covering release, security, operations, compliance, and reliability—with checklists you can track—see **`docs/production-readiness/README.md`**.

---

## 2) Required preconditions

- .NET SDK 10.x installed (repository is pinned via `global.json`).
- .NET 8 runtime/targeting support available for `net8.0` test/build lanes.
- Repository checked out cleanly.
- No local uncommitted production code modifications.
- Optional: `NEXO_PIPELINE_STORE_PROVIDER` and `NEXO_PIPELINE_STORE_PATH` available for durable resume validation.

---

## 3) Gate criteria (PASS/FAIL)

### A. Build and compatibility (required)

1. `Nexo.Core.Application` builds for `netstandard2.0`.
2. `Nexo.Infrastructure` builds.
3. `Nexo.CLI` builds.

**Fail conditions**

- Any build command returns non-zero.
- Any compile-time errors.

---

### B. Pipeline runtime correctness (required)

1. Pipeline-focused infrastructure tests pass on `net8.0`.
2. Pipeline-focused infrastructure tests pass on `net10.0`.
3. Host DI smoke checks pass for default `AddNexo` registration with pipeline layer present.

**Fail conditions**

- Any of the above test commands return non-zero.

---

### C. CLI operational correctness (required)

1. `pipeline validate` succeeds for a valid template.
2. `pipeline run` succeeds and emits completed run output.
3. `pipeline run` fallback path works (deterministic failure -> agentic success).

**Fail conditions**

- Validation fails for known-good template.
- Run command exits non-zero for expected success path.
- Fallback path does not switch workers as expected.

---

### D. Durable resume correctness (required for production readiness)

Using `LiteDb` provider:

1. Create a failed run in one process invocation.
2. Resume from that run in a second process invocation.
3. Resumed run completes successfully.

**Fail conditions**

- Prior run not persisted/readable.
- Resume run cannot load source run.
- Resume path fails to recover and complete.

---

### E. Exceptions policy (required)

Any High/Critical exception to this gate requires:

- owner
- expiration date
- mitigation plan
- explicit sign-off

If missing any of the above -> **FAIL**.

---

## 4) Exact command set (reference implementation)

Run from repo root.

### 4.1 Build checks

```bash
dotnet build src/Nexo.Core.Application/Nexo.Core.Application.csproj -f netstandard2.0
dotnet build src/Nexo.Infrastructure/Nexo.Infrastructure.csproj
dotnet build application/src/Nexo.CLI/Nexo.CLI.csproj
```

### 4.2 Pipeline correctness checks

```bash
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 --filter "FullyQualifiedName~Pipelines"
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net10.0 --filter "FullyQualifiedName~Pipelines"
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 --filter "FullyQualifiedName~HostingE2ESmokeTests.AddNexo_RegistersObservationPipeline_ByDefault|FullyQualifiedName~Pipelines.PipelineServiceCollectionExtensionsTests.AddNexo_RegistersPipelineCompositionLayerByDefault"
```

### 4.3 CLI operational checks

Create a temporary template file:

```bash
cat > /tmp/pipeline_gate_demo.json <<'JSON'
{
  "templateId": "gate-demo",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON
```

Validate and run:

```bash
dotnet run --project application/src/Nexo.CLI -- pipeline validate --template /tmp/pipeline_gate_demo.json
dotnet run --project application/src/Nexo.CLI -- pipeline run --template /tmp/pipeline_gate_demo.json --run-id gate-run-success --format-json
NEXO_PIPELINE_ENABLE_TEST_HOOKS=1 NEXO_PIPELINE_COMPLETION_POLICY=AllowNonCriticalStageFailures dotnet run --project application/src/Nexo.CLI -- pipeline run --template /tmp/pipeline_gate_demo.json --run-id gate-run-fallback --input "fail:hybrid:deterministic=true" --format-json
dotnet run --project application/src/Nexo.CLI -- pipeline diagnostics --format-json
```

### 4.4 Durable resume checks (LiteDb)

```bash
NEXO_PIPELINE_STORE_PROVIDER=LiteDb NEXO_PIPELINE_STORE_PATH=/tmp/nexo_pipeline_gate_resume.db \
NEXO_PIPELINE_ENABLE_TEST_HOOKS=1 \
dotnet run --project application/src/Nexo.CLI -- pipeline run --template /tmp/pipeline_gate_demo.json --run-id gate-resume-source --input "fail:ingest:deterministic=true" --format-json

NEXO_PIPELINE_STORE_PROVIDER=LiteDb NEXO_PIPELINE_STORE_PATH=/tmp/nexo_pipeline_gate_resume.db \
dotnet run --project application/src/Nexo.CLI -- pipeline run --template /tmp/pipeline_gate_demo.json --run-id gate-resume-target --resume-run-id gate-resume-source --resume-failed-stages --format-json
```

Cleanup:

```bash
rm -f /tmp/pipeline_gate_demo.json /tmp/nexo_pipeline_gate_resume.db
```

---

## 5) CI integration recommendation

Add a dedicated workflow (recommended name: `production-readiness-gate-v1.yml`) that runs:

1. Build checks.
2. Pipeline tests for net8/net9.
3. Host DI smoke filter.
4. CLI validate/run/fallback checks.
5. Durable resume checks (LiteDb).

The workflow should:

- upload logs/artifacts (`trx`, command output),
- fail-fast on required command failures,
- emit a single PASS/FAIL summary.

---

## 6) Exit checklist for release candidate

Release candidate can proceed only when all are true:

- [ ] All sections A-D pass.
- [ ] No unapproved High/Critical exceptions.
- [ ] CI gate green on target branch.
- [ ] Rollback plan documented and tested.

---

## 7) Environment setup gate (recommended second gate)

Run `environment-setup-gate-v1` in GitHub Actions to validate dependency bootstrap and NuGet restore for each platform:

- `ubuntu-latest`: `scripts/setup/setup.sh check` + `scripts/setup/setup.sh restore`
- `macos-latest`: `scripts/setup/setup.sh check` + `scripts/setup/setup.sh restore`
- `windows-latest`: `scripts/setup/setup.ps1 -Mode check` + `scripts/setup/setup.ps1 -Mode restore`

This gate validates the repo can be prepared cleanly on each OS before functional/runtime gates run.
