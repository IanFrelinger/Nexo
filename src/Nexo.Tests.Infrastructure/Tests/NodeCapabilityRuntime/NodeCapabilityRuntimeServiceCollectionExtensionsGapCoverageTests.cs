using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.Execution.Agentic;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Sdk;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public sealed class NodeCapabilityRuntimeServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddNodeCapabilityRuntimeCore_registers_runtime_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNodeCapabilityRuntimeCore(BuildConfiguration(new Dictionary<string, string?>()));

        services.Should().Contain(sd => sd.ServiceType == typeof(IHardwareProfiler));
        services.Should().Contain(sd => sd.ServiceType == typeof(IModelServingBackend));
        services.Should().Contain(sd => sd.ServiceType == typeof(IModelLifecycleManager));
        services.Should().Contain(sd => sd.ServiceType == typeof(ModelScoringService));
        services.Should().Contain(sd => sd.ServiceType == typeof(INodeCapabilityRuntime));
        services.Should().Contain(sd => sd.ServiceType == typeof(IAgenticBrickEngine));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType == typeof(NcrStartupHealthService));
    }

    [Fact]
    public void AddNodeCapabilityRuntimeCore_rejects_null_dependencies()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var actServices = () => NodeCapabilityRuntimeServiceCollectionExtensions.AddNodeCapabilityRuntimeCore(null!, configuration);
        var actConfiguration = () =>
        {
            var services = new ServiceCollection();
            NodeCapabilityRuntimeServiceCollectionExtensions.AddNodeCapabilityRuntimeCore(services, null!);
        };

        actServices.Should().Throw<ArgumentNullException>();
        actConfiguration.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(typeof(WindowsPolicy))]
    [InlineData(typeof(MacOsPolicy))]
    [InlineData(typeof(LinuxPolicy))]
    public void AddNodeCapabilityRuntimeDesktop_registers_policy_and_ollama_backend(Type policyType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:NodeCapabilityRuntime:Ollama:BaseUrl"] = "http://127.0.0.1:11434",
        });

        InvokeDesktopRegistration(services, configuration, policyType);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPlatformPolicy>().Should().BeOfType(policyType);
        provider.GetRequiredService<IModelServingBackend>().Should().BeOfType<OllamaModelServingBackend>();
    }

    [Fact]
    public void AddNodeCapabilityRuntimeiOS_registers_mobile_policy()
    {
        var services = new ServiceCollection();
        services.AddNodeCapabilityRuntimeiOS(BuildConfiguration(new Dictionary<string, string?>()));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPlatformPolicy>().Should().BeOfType<iOSPolicy>();
    }

    [Fact]
    public void AddNodeCapabilityRuntimeAndroid_registers_mobile_policy()
    {
        var services = new ServiceCollection();
        services.AddNodeCapabilityRuntimeAndroid(BuildConfiguration(new Dictionary<string, string?>()));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPlatformPolicy>().Should().BeOfType<AndroidPolicy>();
    }

    private static void InvokeDesktopRegistration(
        IServiceCollection services,
        IConfiguration configuration,
        Type policyType)
    {
        if (policyType == typeof(WindowsPolicy))
            services.AddNodeCapabilityRuntimeWindows(configuration);
        else if (policyType == typeof(MacOsPolicy))
            services.AddNodeCapabilityRuntimeMacOS(configuration);
        else if (policyType == typeof(LinuxPolicy))
            services.AddNodeCapabilityRuntimeLinux(configuration);
        else
            throw new ArgumentOutOfRangeException(nameof(policyType));
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
