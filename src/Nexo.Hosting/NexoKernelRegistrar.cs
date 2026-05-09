using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.BackgroundAgents;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Copilot.Ports;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Copilot;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Ephemeral;
using Nexo.Infrastructure.Execution.LoadPolicy;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.Knowledge;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.ModelArtifacts;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.Persistence;
using Nexo.Infrastructure.Persistence.Ephemeral;
using Nexo.Infrastructure.Pipelines;
using Nexo.Orchestration;
using Nexo.Orchestration.Models;
using Nexo.Orchestration.Transport;
using Nexo.Runtime;
using Nexo.Runtime.Routing;
using Nexo.Transport.Grpc;

namespace Nexo.Hosting;

/// <summary>Extracted kernel DI phases from <see cref="NexoServiceCollectionExtensions.AddNexo"/>. Registration order is preserved.</summary>
internal static partial class NexoKernelRegistrar
{
    public static void Register(
        IServiceCollection services,
        NexoHostingOptions options,
        ModuleSelection modules,
        IConfiguration configuration)
    {
        var ctx = new NexoKernelRegistrationContext(services, options, modules, configuration);
        RegisterPhase01_ConfigurationNodeCapabilityRuntime(ctx);
        RegisterPhase02_CQRSMediatRFluentValidation(ctx);
        RegisterPhase03_ConfigurationServiceAdapter(ctx);
        RegisterPhase04_LoopKernelDecoratorChain(ctx);
        RegisterPhase05_OrchestrationTransport(ctx);
        RegisterPhase06_Persistence(ctx);
        RegisterPhase07_Adaptation(ctx);
        RegisterPhase08_CopilotTaskStore(ctx);
        RegisterPhase09_KnowledgeQueryService(ctx);
        RegisterPhase10_PipelineComposition(ctx);
        RegisterPhase11_BackgroundAgentsRAG(ctx);
        RegisterPhase12_ObservationPipeline(ctx);
        RegisterPhase13_ModelDecoratorChain(ctx);
        RegisterPhase14_EphemeralLifecycle(ctx);
        RegisterPhase15_TrustProviderFactory3wayBranching(ctx);
        RegisterPhase16_ExecutionCoreWorkflow(ctx);
        RegisterPhase17_WorkflowExecutor(ctx);
        RegisterPhase18_AnalysisValidation(ctx);
        RegisterPhase19_TestingAdapters(ctx);
        RegisterPhase20_AnalysisRuleEngine(ctx);
    }
}
