using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.Runtime;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Nexo.Runtime.Barriers.Sinks;
using Nexo.Runtime.Routing;
using Nexo.Runtime.Transport;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Runtime;

public sealed class RuntimeServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddNexoRuntimeTransport_registers_routing_composition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoRuntimeTransport<StubInProcessTransport, StubRemoteTransport>();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAgentTransport>().Should().BeOfType<RoutingAgentTransport>();
        provider.GetRequiredService<StubInProcessTransport>().Should().NotBeNull();
        provider.GetRequiredService<StubRemoteTransport>().Should().NotBeNull();
    }

    [Fact]
    public void AddBarrierAuditSinks_rejects_null_dependencies()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var actServices = () => RuntimeServiceCollectionExtensions.AddBarrierAuditSinks(null!, configuration);
        var actConfiguration = () =>
        {
            var services = new ServiceCollection();
            RuntimeServiceCollectionExtensions.AddBarrierAuditSinks(services, null!);
        };

        actServices.Should().Throw<ArgumentNullException>();
        actConfiguration.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBarrierAuditSinks_NoOp_registers_sink()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "NoOp",
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierAuditSink>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<NoOpBarrierAuditSink>();
    }

    [Fact]
    public void AddBarrierIdentityResolvers_rejects_null_dependencies()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var actServices = () => RuntimeServiceCollectionExtensions.AddBarrierIdentityResolvers(null!, configuration);
        var actConfiguration = () =>
        {
            var services = new ServiceCollection();
            RuntimeServiceCollectionExtensions.AddBarrierIdentityResolvers(services, null!);
        };

        actServices.Should().Throw<ArgumentNullException>();
        actConfiguration.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBarrierIdentityResolvers_registers_pki_certificate_resolver()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "public",
            ["Nexo:Barriers:Levels:1"] = "internal",
            ["Nexo:Identity:ResolverPriority:0"] = "PkiCertificate",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });
        services.AddBarrierIdentityResolvers(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierIdentityResolver>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<PkiCertificateBarrierResolver>();
    }

    [Fact]
    public void AddBarrierIdentityResolvers_skips_blank_priority_entries()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "public",
            ["Nexo:Barriers:Levels:1"] = "internal",
            ["Nexo:Identity:ResolverPriority:0"] = "   ",
            ["Nexo:Identity:ResolverPriority:1"] = "ApiKey",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });
        services.AddBarrierIdentityResolvers(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierIdentityResolver>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ApiKeyBarrierResolver>();
    }

    [Fact]
    public void EnsureBarrierLevels_noop_when_levels_null()
    {
        var ensure = typeof(RuntimeServiceCollectionExtensions).GetMethod(
            "EnsureBarrierLevels",
            BindingFlags.Static | BindingFlags.NonPublic);
        ensure.Should().NotBeNull();

        var options = new BarrierOptions();
        typeof(BarrierOptions).GetProperty(nameof(BarrierOptions.Levels))!
            .SetValue(options, null);

        ensure!.Invoke(null, [options]);

        options.Levels.Should().BeNull();
    }

    [Fact]
    public void EnsureBarrierLevels_deduplicates_levels_case_insensitively()
    {
        var ensure = typeof(RuntimeServiceCollectionExtensions).GetMethod(
            "EnsureBarrierLevels",
            BindingFlags.Static | BindingFlags.NonPublic);
        ensure.Should().NotBeNull();

        var options = new BarrierOptions
        {
            Levels = ["public", "PUBLIC", "internal", " internal "],
        };

        ensure!.Invoke(null, [options]);

        options.Levels.Should().Equal("public", "internal");
    }

    [Fact]
    public void AddBarrierAuditSinks_skips_blank_sink_names()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "   ",
            ["Nexo:Audit:Sinks:1"] = "NoOp",
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierAuditSink>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<NoOpBarrierAuditSink>();
    }

    [Fact]
    public void AddBarrierIdentityResolvers_registers_multiple_resolvers_in_priority_order()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "public",
            ["Nexo:Barriers:Levels:1"] = "internal",
            ["Nexo:Identity:ResolverPriority:0"] = "ApiKey",
            ["Nexo:Identity:ResolverPriority:1"] = "JwtClaim",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });
        services.AddBarrierIdentityResolvers(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierIdentityResolver>()
            .Select(r => r.GetType())
            .Should()
            .Equal(typeof(ApiKeyBarrierResolver), typeof(JwtClaimBarrierResolver));
    }

    [Fact]
    public void AddNexoRuntimeRouting_registers_endpoint_registry_and_health_monitor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoRuntimeRouting(BuildConfiguration(new Dictionary<string, string?>()));

#if NET8_0_OR_GREATER
        services.Should().Contain(descriptor => descriptor.ImplementationType == typeof(EndpointHealthMonitor));
#endif

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEndpointRegistry>().Should().BeOfType<InMemoryEndpointRegistry>();
    }

    [Fact]
    public void AddNexoRuntimeRouting_blank_barrier_levels_use_defaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "   ",
            ["Nexo:Barriers:Levels:1"] = "",
        });
        services.AddNexoRuntimeRouting(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value.Levels
            .Should()
            .ContainInOrder("public", "internal");
    }

    [Fact]
    public void AddNexoRuntimeRouting_registers_barrier_runtime_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexoRuntimeRouting(BuildConfiguration(new Dictionary<string, string?>()));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IBarrierContextAmbient>().Should().BeOfType<BarrierContextAmbient>();
        provider.GetRequiredService<IBarrierContextAccessor>().Should().BeOfType<ScopedBarrierContextAccessor>();
        provider.GetRequiredService<IBarrierAuditLog>().Should().BeOfType<StructuredBarrierAuditLog>();
        provider.GetRequiredService<IBarrierIdentityResolverPipeline>()
            .Should()
            .BeOfType<DefaultBarrierIdentityResolverPipeline>();
    }

    [Fact]
    public void AddBarrierAuditSinks_empty_sink_list_registers_noop()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(BuildConfiguration(new Dictionary<string, string?>()));

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierAuditSink>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<NoOpBarrierAuditSink>();
    }

    [Fact]
    public void AddBarrierAuditSinks_unknown_sink_throws()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "UnknownSink",
        });
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddBarrierAuditSinks(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown audit sink*UnknownSink*");
    }

    [Fact]
    public void AddBarrierAuditSinks_structured_log_registers_sink()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "StructuredLog",
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierAuditSink>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<StructuredLogBarrierAuditSink>();
    }

    [Fact]
    public void AddBarrierIdentityResolvers_jwt_claim_resolver_registers()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "public",
            ["Nexo:Barriers:Levels:1"] = "internal",
            ["Nexo:Identity:ResolverPriority:0"] = "JwtClaim",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });
        services.AddBarrierIdentityResolvers(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierIdentityResolver>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<JwtClaimBarrierResolver>();
    }

    [Fact]
    public void AddBarrierIdentityResolvers_unknown_resolver_throws()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Barriers:Levels:0"] = "public",
            ["Nexo:Identity:ResolverPriority:0"] = "UnknownResolver",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });

        var act = () => services.AddBarrierIdentityResolvers(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown barrier identity resolver*UnknownResolver*");
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task AddBarrierAuditSinks_file_registers_sink_and_lifetime()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Nexo:Audit:Sinks:0"] = "File",
            ["Nexo:Audit:File:Directory"] = Path.Combine(Path.GetTempPath(), "nexo-audit-di-" + Guid.NewGuid().ToString("N")),
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBarrierAuditSinks(configuration);

        await using var provider = services.BuildServiceProvider();
        provider.GetServices<IBarrierAuditSink>()
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<FileBarrierAuditSink>();
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .Should()
            .Contain(service => service is FileBarrierAuditSinkLifetime);
    }
#endif

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private sealed class StubInProcessTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "in-process"));
    }

    private sealed class StubRemoteTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "remote"));
    }
}
