# NCR Release SLOs and Alerts (v1)

This document defines the minimum telemetry-backed SLOs for first NCR release.

## What is actually emitted (read this before wiring alerts)

The `ncr.*` names below are **`IMetricsCollector` keys** recorded by `NodeCapabilityRuntime`, `OllamaModelServingBackend` and `BehaviorExecutor` (`RecordExecutionTime(...)` for `*.duration`, `IncrementCounter(...)` for the rest). They are real and stable, but they are **not** exported as metrics named `ncr.*`, and by default they are not exported at all:

- **Default (no `OTEL_EXPORTER_OTLP_ENDPOINT`):** the shipped hosts register the in-process `MemoryMetricsCollector`. Values live only in that process; no shipped host exposes the snapshot over HTTP or a scrape endpoint. Nothing in the tables below is observable from outside the process in this mode.
- **With `OTEL_EXPORTER_OTLP_ENDPOINT` set (Nexo.API):** `AddNexoOpenTelemetry` swaps in `OpenTelemetryMetricsCollector`, whose `Nexo` meter emits exactly two instruments — `nexo.operation.duration` (histogram, unit `ms`, attribute `operation=<key>`) and `nexo.operation.count` (counter, attribute `counter=<key>`). Every `ncr.*` name in this document therefore appears as an **attribute value**, e.g. `nexo.operation.duration{operation="ncr.model_resolution.duration"}` or `nexo.operation.count{counter="ncr.model_load.error"}`. Write your PromQL / OTel queries against those two instruments filtered by attribute; the p95 targets refer to the `nexo.operation.duration` histogram sliced by `operation`.
- **`ncr.execution.escalated.*` and `ncr.model_resolution.target.*` / `.reason.*`** are counter families with the enum value appended (`ncr.execution.escalated.EscalatedPolicyBlocked`, ...); they surface as distinct `counter` attribute values on `nexo.operation.count`.
- **`CapabilitiesFetchResult.IsStale`** (item 6) is a return-value flag, not a metric; nothing emits it today. Treat that row as a suggested signal until a counter is added.

Configuration for both modes is in `docs/Configuration.md` § Observability; compose usage in `docs/DEPLOYMENT.md` § Observability. The `nexo runtime gate` / `runtime release-gate` SLO evidence (`ncr.model_resolution.p95_ms`, `ncr.model_load.p95_ms`, `ncr.outcome.p95_ms`) is computed from persisted runtime history by the CLI and is unrelated to the exporter.

## Scope

Applies to:
- NCR model resolution (`ncr.model_resolution.*`)
- NCR model lifecycle load (`ncr.model_load.*`)
- NCR execution outcomes (`ncr.outcome.*`)
- Ollama backend integration (`ncr.ollama.*`)
- Agentic escalation signals (`ncr.execution.escalated.*`)

### Virtual production harness (routing stack)

For CI and local soak without GPUs or live peer meshes, **`VirtualProductionNcrRoutingHost`** (`Nexo.Tests.Infrastructure/Helpers/Ncr/VirtualProductionNcrRoutingHost.cs`) boots a generic **`IHost`** with **`AddRunPodCapabilityRouting`** wiring production types: **`RunPodHttpClient`** (HTTP to **`RunPodLoopbackApiServer`** — same `/v2/*` paths as cloud), **`ProviderFactoryLocalExecutor`** + **`ProviderFactory`**, **`EnvironmentHardwareProfiler`**, **`EnvironmentQueueDepthProvider`**, **`FileBasedInstanceDiscovery`** (temp mesh JSON), and **`BrickRegistry`**. Integration tests: **`VirtualProductionNcrRoutingTests`**.

## SLO targets (first release)

Evaluate over 1h rolling windows in production-like environments.

1. Model resolution latency
- Metric: `ncr.model_resolution.duration`
- Target: p95 <= 150ms
- Alert: p95 > 300ms for 15m

2. Model load reliability
- Metrics: `ncr.model_load.success`, `ncr.model_load.error`
- Target: error ratio <= 2%
- Alert: error ratio > 5% for 10m

3. Inference reliability
- Metrics: `ncr.ollama.chat.success`, `ncr.ollama.chat.failure`, `ncr.ollama.chat.error`
- Target: failure+error ratio <= 2%
- Alert: failure+error ratio > 5% for 10m

4. Inference latency
- Metric: `ncr.ollama.chat.duration`
- Target: p95 <= 8s for interactive workloads
- Alert: p95 > 12s for 15m

5. Escalation pressure
- Metric family: `ncr.execution.escalated.*`
- Target: escalation ratio <= 10% of agentic attempts
- Alert: escalation ratio > 20% for 15m

6. Stale capability dependence
- Signal: `CapabilitiesFetchResult.IsStale == true` (from remote catalog fetches)
- Target: stale responses <= 10% of capability fetches
- Alert: stale responses > 25% for 15m

## Operational interpretation

- High `ncr.execution.escalated.EscalatedPolicyBlocked` often means local constraints or policy mismatch.
- High `ncr.execution.escalated.EscalatedInsufficientMemory` suggests memory pressure or model sizing mismatch.
- Simultaneous rises in `ncr.ollama.ps.error` and `ncr.model_load.error` usually indicate backend health degradation.

## Release gate recommendation

Before first release sign-off:
- Run a 30-minute soak with representative workflows.
- Confirm all SLO targets are met for the full soak window.
- No unresolved Critical alerts at sign-off time.
