using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Ashlar.Abstractions.Routing;
using Ashlar.Abstractions.Transport;
using Ashlar.BackgroundAgents;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Analysis.UseCases.AnalyzeCode;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Common.Services;
using Ashlar.Core.Application.Copilot.Ports;
using Ashlar.Core.Application.Ephemeral.Ports;
using Ashlar.Core.Application.Knowledge.Ports;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.Testing.UseCases.RunTests;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Application.Validation.UseCases.RunValidation;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Copilot;
using Ashlar.Infrastructure.Execution;
using Ashlar.Infrastructure.Execution.Ephemeral;
using Ashlar.Infrastructure.Execution.LoadPolicy;
using Ashlar.Infrastructure.Execution.Routing;
using Ashlar.Infrastructure.Knowledge;
using Ashlar.Infrastructure.Maintenance;
using Ashlar.Infrastructure.ModelArtifacts;
using Ashlar.Infrastructure.NodeCapabilityRuntime;
using Ashlar.Infrastructure.Persistence;
using Ashlar.Infrastructure.Persistence.Ephemeral;
using Ashlar.Infrastructure.Pipelines;
using Ashlar.Orchestration;
using Ashlar.Orchestration.Models;
using Ashlar.Orchestration.Transport;
using Ashlar.Runtime;
using Ashlar.Runtime.Routing;
using Ashlar.Transport.Grpc;

namespace Ashlar.Hosting;

/// <summary>Extracted kernel DI phases from <see cref="AshlarServiceCollectionExtensions.AddAshlar"/>. Registration order is preserved.</summary>
internal static partial class AshlarKernelRegistrar
{
    public static void Register(
        IServiceCollection services,
        AshlarHostingOptions options,
        ModuleSelection modules,
        IConfiguration configuration)
    {
        var ctx = new AshlarKernelRegistrationContext(services, options, modules, configuration);
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
        RegisterPhase13b_MeaiPipeline(ctx);
        RegisterPhase14_EphemeralLifecycle(ctx);
        RegisterPhase15_TrustProviderFactory3wayBranching(ctx);
        RegisterPhase16_ExecutionCoreWorkflow(ctx);
        RegisterPhase17_WorkflowExecutor(ctx);
        RegisterPhase18_AnalysisValidation(ctx);
        RegisterPhase19_TestingAdapters(ctx);
        RegisterPhase20_AnalysisRuleEngine(ctx);
    }
}
