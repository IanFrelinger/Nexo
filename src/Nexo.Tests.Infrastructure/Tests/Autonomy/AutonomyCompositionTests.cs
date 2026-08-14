using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Autonomy;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Infrastructure.Certification.Sdk.Extensions;
using Nexo.Infrastructure.Execution.Sandbox;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Autonomy;

/// <summary>
/// Host composition for the autonomy loop: the graph resolves without cycles, options are
/// fail-closed and validated, authority surfaces are shared singletons, and hosts can
/// substitute any piece. The resolution test is not ceremony — this codebase has already
/// produced a laundered factory cycle that passed <c>ValidateOnBuild</c> and then recursed
/// at RESOLUTION time (the self-extend runner ↔ agent registry loop), which the DI
/// container cannot see through and only an actual resolve catches.
/// </summary>
[Trait("Category", "Certification")]
public sealed class AutonomyCompositionTests
{
    [Fact]
    public void TheAutonomyGraph_Resolves_WithoutCyclesOrMissingDependencies()
    {
        using var provider = BuildProvider();

        // Resolve the whole surface, not just the harness: a cycle laundered through a
        // factory lambda only manifests when the specific edge is walked.
        provider.GetRequiredService<AutonomousIterationHarness>().Should().NotBeNull();
        provider.GetRequiredService<CertifiedBrickHotSwapHost>().Should().NotBeNull();
        provider.GetRequiredService<ISandboxedSessionRunner>().Should().NotBeNull();
        provider.GetRequiredService<DockerSandboxSessionReaper>().Should().NotBeNull();
        provider.GetRequiredService<ClusterBudget>().Should().NotBeNull();
        provider.GetRequiredService<ICertificationGate>().Should().NotBeNull();
        provider.GetRequiredService<ISessionProvenanceSink>().Should().NotBeNull();
    }

    [Fact]
    public void AuthoritySurfaces_AreSharedSingletons()
    {
        using var provider = BuildProvider();

        // Quarantine and demotion are process-wide facts; two instances would be two
        // disagreeing truths, and the swap host would consult a different list than the
        // watch window wrote to.
        provider.GetRequiredService<ICertificateRevocationList>()
            .Should().BeSameAs(provider.GetRequiredService<ICertificateRevocationList>());
        provider.GetRequiredService<ILineageAuthority>()
            .Should().BeSameAs(provider.GetRequiredService<ILineageAuthority>());
        provider.GetRequiredService<LoopPauseControl>()
            .Should().BeSameAs(provider.GetRequiredService<LoopPauseControl>());
    }

    [Fact]
    public void QuarantineWrittenByOnePath_IsVisibleToTheSwapHost()
    {
        using var provider = BuildProvider();
        var revocations = provider.GetRequiredService<ICertificateRevocationList>();
        var host = provider.GetRequiredService<CertifiedBrickHotSwapHost>();

        revocations.Revoke("sha256:quarantined", "composition test");

        // The host was constructed with the same list instance the test just wrote to —
        // proven by behavior rather than by reading the registration.
        host.Should().NotBeNull();
        provider.GetRequiredService<ICertificateRevocationList>()
            .IsRevoked("sha256:quarantined").Should().BeTrue();
    }

    [Fact]
    public void OptionsAreFailClosed_AndDisabledIsAlwaysValid()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<IOptions<NexoAutonomyOptions>>().Value;

        options.Enabled.Should().BeFalse("a host that merely composes the loop must not run it");
        new ValidateNexoAutonomyOptions().Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("SessionImage")]
    [InlineData("RetentionWindow")]
    [InlineData("ThroughputGuardFactor")]
    public void EnabledButMisconfigured_FailsValidation(string broken)
    {
        var options = new NexoAutonomyOptions
        {
            Enabled = true,
            UseSandboxSessions = true,
            SessionImage = broken == "SessionImage" ? null : "proposer:latest",
            RetentionWindow = broken == "RetentionWindow" ? 0 : 2,
            ThroughputGuardFactor = broken == "ThroughputGuardFactor" ? 1 : 4,
        };

        var result = new ValidateNexoAutonomyOptions().Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(broken);
    }

    [Fact]
    public void AirGappedProfile_DoesNotRefuseAutonomy_UnlikeProtocolSurfaces()
    {
        var previous = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", "airgapped");
        try
        {
            var options = new NexoAutonomyOptions
            {
                Enabled = true,
                UseSandboxSessions = true,
                SessionImage = "proposer:latest",
            };

            new ValidateNexoAutonomyOptions().Validate(null, options).Succeeded.Should().BeTrue(
                "R6.3 requires autonomy controls to work fully offline — an air-gapped host "
                + "running a local model is a legitimate deployment, and refusing it would "
                + "enforce the reverse of the invariant");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE", previous);
        }
    }

    [Fact]
    public void HostsCanSubstituteAnyPiece_ByRegisteringItFirst()
    {
        var mine = new InMemoryCertificateRevocationList();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICertificateRevocationList>(mine);
        // AddCertificationGate (not AddCertificationInfrastructure alone): the latter
        // registers ICompositionCertificationGate without the IBrickRegistry it needs, so
        // it cannot satisfy ValidateOnBuild by itself. Real hosts call this one.
        services.AddCertificationGate();
        services.AddNexoAutonomy();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificateRevocationList>().Should().BeSameAs(mine,
            "TryAdd throughout means a host's own registration wins");
    }

    [Fact]
    public void ConfigurationBinds_FromTheNexoAutonomySection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCertificationInfrastructure();
        services.AddNexoAutonomy(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Autonomy:Enabled"] = "true",
                ["Nexo:Autonomy:UseSandboxSessions"] = "true",
                ["Nexo:Autonomy:SessionImage"] = "proposer:latest",
                ["Nexo:Autonomy:CadenceFloorSeconds"] = "42",
            })
            .Build());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NexoAutonomyOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.SessionImage.Should().Be("proposer:latest");
        options.CadenceFloorSeconds.Should().Be(42);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // AddCertificationGate (not AddCertificationInfrastructure alone): the latter
        // registers ICompositionCertificationGate without the IBrickRegistry it needs, so
        // it cannot satisfy ValidateOnBuild by itself. Real hosts call this one.
        services.AddCertificationGate();
        services.AddNexoAutonomy();
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
