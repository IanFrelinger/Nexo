using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Hosting;

/// <summary>
/// Flags produced by <see cref="NexoServiceCollectionExtensions.GetModuleSelection"/> that decide which subsystem
/// modules are registered.
/// </summary>
internal sealed record ModuleSelection(
    bool IncludeNodeCapabilityRuntime,
    bool IncludeRuntimeTransport,
    bool IncludePersistence,
    bool IncludeAdaptation,
    bool IncludePipelineComposition,
    bool IncludeBackgroundAgents,
    bool IncludeBackgroundAgentRag,
    bool IncludeObservationPipeline,
    bool IncludeTrustServices,
    bool IncludeSkills,
    bool IncludeWorkflowIntegrations,
    bool IncludeTestingAdapters);
