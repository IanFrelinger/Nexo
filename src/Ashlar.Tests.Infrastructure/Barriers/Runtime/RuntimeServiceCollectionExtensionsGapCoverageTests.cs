using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;
using Ashlar.Abstractions.Routing;
using Ashlar.Abstractions.Transport;
using Ashlar.Runtime;
using Ashlar.Runtime.Barriers;
using Ashlar.Runtime.Barriers.Identity;
using Ashlar.Runtime.Barriers.Identity.Resolvers;
using Ashlar.Runtime.Barriers.Sinks;
using Ashlar.Runtime.Routing;
using Ashlar.Runtime.Transport;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Barriers.Runtime;

/// <summary>Tests for runtime service collection extensions gap coverage.</summary>
public sealed class RuntimeServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddAshlarRuntimeTransport_registers_routing_composition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlarRuntimeTransport<StubInProcessTransport, StubRemoteTransport>();

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
            ["Ashlar:Audit:Sinks:0"] = "NoOp",
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
            ["Ashlar:Barriers:Levels:0"] = "public",
            ["Ashlar:Barriers:Levels:1"] = "internal",
            ["Ashlar:Identity:ResolverPriority:0"] = "PkiCertificate",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Ashlar:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            /// <summary>Barrier hierarchy.</summary>
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
            ["Ashlar:Barriers:Levels:0"] = "public",
            ["Ashlar:Barriers:Levels:1"] = "internal",
            ["Ashlar:Identity:ResolverPriority:0"] = "   ",
            ["Ashlar:Identity:ResolverPriority:1"] = "ApiKey",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Ashlar:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            /// <summary>Barrier hierarchy.</summary>
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
            ["Ashlar:Audit:Sinks:0"] = "   ",
            ["Ashlar:Audit:Sinks:1"] = "NoOp",
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
            ["Ashlar:Barriers:Levels:0"] = "public",
            ["Ashlar:Barriers:Levels:1"] = "internal",
            ["Ashlar:Identity:ResolverPriority:0"] = "ApiKey",
            ["Ashlar:Identity:ResolverPriority:1"] = "JwtClaim",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Ashlar:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            /// <summary>Barrier hierarchy.</summary>
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
    public void AddAshlarRuntimeRouting_registers_endpoint_registry_and_health_monitor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlarRuntimeRouting(BuildConfiguration(new Dictionary<string, string?>()));

#if NET8_0_OR_GREATER
        services.Should().Contain(descriptor => descriptor.ImplementationType == typeof(EndpointHealthMonitor));
#endif

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEndpointRegistry>().Should().BeOfType<InMemoryEndpointRegistry>();
    }

    [Fact]
    public void AddAshlarRuntimeRouting_blank_barrier_levels_use_defaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ashlar:Barriers:Levels:0"] = "   ",
            ["Ashlar:Barriers:Levels:1"] = "",
        });
        services.AddAshlarRuntimeRouting(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value.Levels
            .Should()
            .ContainInOrder("public", "internal");
    }

    [Fact]
    public void AddAshlarRuntimeRouting_registers_barrier_runtime_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlarRuntimeRouting(BuildConfiguration(new Dictionary<string, string?>()));

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
            ["Ashlar:Audit:Sinks:0"] = "UnknownSink",
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
            ["Ashlar:Audit:Sinks:0"] = "StructuredLog",
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
            ["Ashlar:Barriers:Levels:0"] = "public",
            ["Ashlar:Barriers:Levels:1"] = "internal",
            ["Ashlar:Identity:ResolverPriority:0"] = "JwtClaim",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Ashlar:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            /// <summary>Barrier hierarchy.</summary>
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
            ["Ashlar:Barriers:Levels:0"] = "public",
            ["Ashlar:Identity:ResolverPriority:0"] = "UnknownResolver",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Ashlar:Barriers").Bind(options));
        services.AddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            /// <summary>Barrier hierarchy.</summary>
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
            ["Ashlar:Audit:Sinks:0"] = "File",
            ["Ashlar:Audit:File:Directory"] = Path.Combine(Path.GetTempPath(), "ashlar-audit-di-" + Guid.NewGuid().ToString("N")),
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

    /// <summary>Stub in process transport.</summary>
    private sealed class StubInProcessTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "in-process"));
    }

    /// <summary>Stub remote transport.</summary>
    private sealed class StubRemoteTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(Success: true));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "remote"));
    }
}
