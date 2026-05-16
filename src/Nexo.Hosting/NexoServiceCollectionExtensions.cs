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

/// <summary>
/// DI composition root for the Nexo kernel.  This is the single place that wires every
/// subsystem together — orchestration, adaptation, persistence, trust, execution, etc.
/// <para>
/// <b>Architecture:</b> The method <see cref="AddNexo"/> follows a strict registration
/// order because later registrations depend on services registered earlier (e.g. the
/// model decorator chain wraps <c>ProviderBackedModel → HotSwappableModel →
/// OrchestrationRuntimeModelDecorator</c>, so the provider factory must already exist).
/// </para>
/// <para>
/// <b>Deployment profiles:</b> A <see cref="NexoDeploymentProfile"/> (resolved from
/// <c>NEXO_DEPLOYMENT_PROFILE</c> or <see cref="NexoHostingOptions.DeploymentProfile"/>)
/// controls which subsystem modules are included via <see cref="ModuleSelection"/>.
/// Profiles range from <c>Full</c> (all modules) down to <c>System</c> (bare minimum
/// for CLI/headless tooling).
/// </para>
/// <para>
/// <b>Related files:</b>
/// <see cref="NexoHostingOptions"/> — caller-facing option bag;
/// <see cref="NexoDeploymentProfile"/> — deployment tier enum;
/// <c>Nexo.Core.Domain.NexoDefaults</c> — all tuneable default constants.
/// </para>
/// </summary>
public static partial class NexoServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nexo with an explicit deployment profile.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="profile">Dependency profile to apply.</param>
    /// <param name="configure">Optional additional options overrides.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoProfile(
        this IServiceCollection services,
        NexoDeploymentProfile profile,
        Action<NexoHostingOptions>? configure = null)
    {
        return services.AddNexo(options =>
        {
            options.DeploymentProfile = profile;
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers every Nexo subsystem into the DI container.  The registration order
    /// matters: downstream registrations (model decorator chain, workflow executor)
    /// resolve services registered in earlier blocks.
    /// <para>
    /// <b>Environment variables read here (see inline comments for each):</b>
    /// <c>NEXO_STRICT_MODE</c>, <c>NEXO_DEPLOYMENT_PROFILE</c>,
    /// <c>NEXO_LOOP_PARALLEL</c>, <c>NEXO_LOOP_INSTRUMENT</c>,
    /// <c>NEXO_OBSERVATION_FAIL_OPEN</c>, <c>NEXO_EPHEMERAL</c>,
    /// <c>NEXO_EPHEMERAL_MODELS</c>, <c>NEXO_EPHEMERAL_DB</c>,
    /// <c>NEXO_TRUST_ENABLED</c>, <c>NEXO_LOAD_PREFERENCE</c>,
    /// <c>NEXO_EXECUTION_REMOTE_URL</c>.
    /// </para>
    /// </summary>
    public static IServiceCollection AddNexo(
        this IServiceCollection services,
        Action<NexoHostingOptions>? configure = null)
    {
        var options = new NexoHostingOptions();
        configure?.Invoke(options);
        ResolveStrictMode(options);
        var deploymentProfile = ResolveDeploymentProfile(options);
        var modules = GetModuleSelection(deploymentProfile);

        services.AddSingleton(options.StrictMode);

        services.AddHttpClient();
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        NexoKernelRegistrar.Register(services, options, modules, configuration);

        return services;
    }
}

