using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions;
using Nexo.Abstractions.Routing;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Hosting;
using Nexo.Infrastructure.Execution;
using Nexo.Runtime.Routing;
using Nexo.Tests.Infrastructure.Helpers;
using Nexo.Transport.Grpc;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Hosting;

/// <summary>
/// Maps <see cref="NexoKernelRegistrar"/> phases to resolvable services per
/// <see cref="NexoDeploymentProfile"/>. See docs/architecture/KernelPhaseMatrix.md.
/// </summary>
[Trait("Category", "ProdStyle")]
[Trait("Category", "E2E")]
[Collection("EnvironmentVariables")]
public sealed class KernelPhaseResolutionTests
{
    public static TheoryData<NexoDeploymentProfile, KernelProfileExpectations> ProfileMatrix =>
        new()
        {
            { NexoDeploymentProfile.Full, KernelProfileExpectations.Full },
            { NexoDeploymentProfile.Server, KernelProfileExpectations.Full },
            { NexoDeploymentProfile.Edge, KernelProfileExpectations.Edge },
            { NexoDeploymentProfile.AirGapped, KernelProfileExpectations.AirGapped },
            { NexoDeploymentProfile.System, KernelProfileExpectations.System },
        };

    [Theory(Timeout = TestTimeouts.E2E)]
    [MemberData(nameof(ProfileMatrix))]
    public async Task Profile_ResolvesExpectedKernelServices(
        NexoDeploymentProfile profile,
        KernelProfileExpectations expected)
    {
        await Task.CompletedTask;
        var sp = BuildProvider(profile);

        AssertPresence(sp, expected.LoopKernel, sp => sp.GetService<ILoopKernel>());
        AssertPresence(sp, expected.ConfigurationService, sp => sp.GetService<IConfigurationService>());
        AssertPresence(sp, expected.Model, sp => sp.GetService<IModel>());
        AssertPresence(sp, expected.ProviderFactory, sp => sp.GetService<IProviderFactory>());
        AssertPresence(sp, expected.PipelineValidator, sp => sp.GetService<IPipelineTemplateValidator>());
        AssertPresence(sp, expected.PatternStore, sp => sp.GetService<IPatternStore>());
        AssertPresence(sp, expected.BackgroundAgents, sp => sp.GetService<IBackgroundAgentRegistry>());
        AssertPresence(sp, expected.GrpcChannelFactory, sp => sp.GetService<IGrpcChannelFactory>());
        AssertPresence(sp, expected.CloudSanitization, sp => sp.GetService<ICloudSanitizationProxy>());

        if (expected.EndpointRegistryNotInMemory)
        {
            sp.GetRequiredService<IEndpointRegistry>().Should().NotBeOfType<InMemoryEndpointRegistry>();
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task FullProfile_LoopKernel_IsSequentialByDefault()
    {
        await Task.CompletedTask;
        var sp = BuildProvider(NexoDeploymentProfile.Full);
        var loop = sp.GetRequiredService<ILoopKernel>();
        loop.Should().BeOfType<SequentialLoopKernel>();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task EdgeProfile_ValidatesMinimalPipelineTemplate()
    {
        await Task.CompletedTask;
        var sp = BuildProvider(NexoDeploymentProfile.Edge);
        var validator = sp.GetRequiredService<IPipelineTemplateValidator>();
        var result = validator.Validate(new PipelineTemplate
        {
            TemplateId = "kernel-gate-edge",
            Version = "1.0",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "a", Name = "A", Mode = PipelineExecutionMode.Deterministic },
            },
        });

        result.IsValid.Should().BeTrue(
            result.Errors is { Count: > 0 } errors ? string.Join("; ", errors) : "validation failed");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task FullProfile_ResolvesKnowledgeQuery_whenObservationEnabled()
    {
        await Task.CompletedTask;
        var sp = BuildProvider(NexoDeploymentProfile.Full);
        sp.GetRequiredService<IKnowledgeQueryService>().Should().NotBeNull();
    }

    private static ServiceProvider BuildProvider(NexoDeploymentProfile profile)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoProfile(profile);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static void AssertPresence<T>(ServiceProvider sp, bool expected, Func<ServiceProvider, T?> resolve)
        where T : class
    {
        if (!expected)
        {
            resolve(sp).Should().BeNull(typeof(T).Name);
            return;
        }

        resolve(sp).Should().NotBeNull(typeof(T).Name);
    }

    public sealed record KernelProfileExpectations(
        bool LoopKernel,
        bool ConfigurationService,
        bool Model,
        bool ProviderFactory,
        bool PipelineValidator,
        bool PatternStore,
        bool BackgroundAgents,
        bool GrpcChannelFactory,
        bool CloudSanitization,
        bool EndpointRegistryNotInMemory)
    {
        public static KernelProfileExpectations Full { get; } = new(
            LoopKernel: true,
            ConfigurationService: true,
            Model: true,
            ProviderFactory: true,
            PipelineValidator: true,
            PatternStore: true,
            BackgroundAgents: true,
            GrpcChannelFactory: true,
            CloudSanitization: true,
            EndpointRegistryNotInMemory: false);

        public static KernelProfileExpectations Edge { get; } = new(
            LoopKernel: true,
            ConfigurationService: true,
            Model: true,
            ProviderFactory: true,
            PipelineValidator: true,
            PatternStore: false,
            BackgroundAgents: false,
            GrpcChannelFactory: false,
            CloudSanitization: false,
            EndpointRegistryNotInMemory: true);

        public static KernelProfileExpectations AirGapped { get; } = new(
            LoopKernel: true,
            ConfigurationService: true,
            Model: true,
            ProviderFactory: true,
            PipelineValidator: true,
            PatternStore: false,
            BackgroundAgents: false,
            GrpcChannelFactory: false,
            CloudSanitization: false,
            EndpointRegistryNotInMemory: true);

        public static KernelProfileExpectations System { get; } = new(
            LoopKernel: true,
            ConfigurationService: true,
            Model: true,
            ProviderFactory: true,
            PipelineValidator: false,
            PatternStore: false,
            BackgroundAgents: false,
            GrpcChannelFactory: false,
            CloudSanitization: false,
            EndpointRegistryNotInMemory: true);
    }
}
