# Kernel phase matrix

Maps `NexoKernelRegistrar` phases (registration order in `NexoKernelRegistrar.cs`) to module flags, primary services, and automated proof.

**Module flags** come from `GetModuleSelection` in `NexoServiceCollectionExtensions.Deployment.cs`.

| Phase | Name | Module gates | Primary services | Automated proof |
|-------|------|--------------|------------------|-----------------|
| 01 | Configuration & NCR | `IncludeNodeCapabilityRuntime` | `RemoteCapabilitiesOptions`, RunPod routing | `KernelPhaseResolutionTests` (Full/AirGapped) |
| 02 | CQRS & validation | Always | MediatR, `ValidationBehavior` | Build + `IValidationService` (Full) |
| 03 | Configuration adapter | Always | `IConfigurationService` | All profiles in `HostingDeploymentProfileTests` |
| 04 | Loop kernel | Always | `ILoopKernel` | `KernelPhaseResolutionTests` all profiles |
| 05 | Orchestration & transport | `IncludeRuntimeTransport` | `IOrchestrationRuntimeSpecAccessor`, `IGrpcChannelFactory` | Transport: `Nexo.Tests.Transport` ProdStyle |
| 06 | Persistence | `IncludePersistence` | LiteDB / Postgres provisioner | Pipeline store tests; prod-readiness resume |
| 07 | Adaptation | `IncludeAdaptation` | `IAdaptationLog`, `IBrickRegistry` | Adaptation Category tests |
| 08 | Copilot task store | Always | `ICopilotTaskStore` | Resolve on Full (optional assert) |
| 09 | Knowledge query | Always (lazy deps) | `IKnowledgeQueryService` | Full profile only in resolution tests |
| 10 | Pipeline composition | `IncludePipelineComposition` | `IPipelineTemplateValidator` | Edge/Full/AirGapped in resolution tests |
| 11 | Background agents & RAG | `IncludeBackgroundAgents`, `IncludeBackgroundAgentRag` | `IBackgroundAgentRegistry` | Full only; absent Edge/System |
| 12 | Observation | `IncludeObservationPipeline` && !`DisableObservationPipeline` | `IPatternStore` | `HostingE2ESmokeTests`, observation integration |
| 13 | Model decorator chain | Always | `IModel`, `HotSwappableModel` | All profiles resolve `IModel` |
| 14 | Ephemeral lifecycle | Env `NEXO_EPHEMERAL*` | `IEphemeralModelLifecycle` | Env-gated; manual |
| 15 | Trust & provider factory | `IncludeTrustServices`, env trust/load | `IProviderFactory`, `ICloudSanitizationProxy` | Trust tests; Full vs AirGapped |
| 16 | Execution core | Always | `IBehaviorExecutor`, `ITextFileSystem` | Workflow executor tests |
| 17 | Workflow executor | Always | `WorkflowExecutor` (scoped) | `WorkflowExecutorIntegrationTests` |
| 18 | Analysis & validation | Always | `IAnalysisService`, `IValidationService` | `HostingE2ESmokeTests.ValidateAsync` |
| 19 | Testing adapters | `IncludeTestingAdapters` | `IExecutionPlatform`, Docker | Full profile; optional remote URL |
| 20 | Analysis rules & fleet | Always (fleet at end) | `IFleetNodeRegistry`, `IMeshTaskRegistry` | Fleet unit tests; mesh lab E2E |

## Deployment profile × modules

| Module | Full | Server | Edge | AirGapped | System |
|--------|:----:|:------:|:----:|:---------:|:------:|
| NCR | ✓ | ✓ | | ✓ | |
| Runtime transport | ✓ | ✓ | | | |
| Persistence | ✓ | ✓ | ✓ | ✓ | |
| Adaptation | ✓ | ✓ | | ✓ | |
| Pipelines | ✓ | ✓ | ✓ | ✓ | |
| Background agents | ✓ | ✓ | | | |
| Observation | ✓ | ✓ | | | |
| Trust services | ✓ | ✓ | | | |
| Workflow integrations | ✓ | ✓ | | | |
| Testing adapters | ✓ | ✓ | | | |

## Gaps (explicit)

| Gap | Owner action |
|-----|----------------|
| Phase 14 ephemeral | Add env-matrix test when ephemeral becomes app-critical |
| Phase 09 on System/Edge | Do not resolve `IKnowledgeQueryService` without `IAdaptationLog` + `IPatternStore` (factory throws) |
| Phase 09 on AirGapped | Requires observation or adaptation pattern store before resolve |
| Mesh federation | Use mesh lab gates, not kernel-gate default |

## Commands

```bash
make kernel-gate
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter "FullyQualifiedName~KernelPhaseResolutionTests"
```
