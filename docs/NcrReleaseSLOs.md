# NCR Release SLOs and Alerts (v1)

This document defines the minimum telemetry-backed SLOs for first NCR release.

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
