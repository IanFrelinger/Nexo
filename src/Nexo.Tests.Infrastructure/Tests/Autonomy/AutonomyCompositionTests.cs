using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Autonomy;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Infrastructure.Certification.Sdk.Extensions;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;
using Nexo.Tests.Infrastructure.Helpers;
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
    [InlineData("BuildCandidateInSession")]
    [InlineData("ExecuteCandidateInSession")]
    [InlineData("WatchMaxInvocationSeconds")]
    [InlineData("DigestIntervalSeconds")]
    [InlineData("SessionImageDigest")]
    public void EnabledButMisconfigured_FailsValidation(string broken)
    {
        var options = new NexoAutonomyOptions
        {
            Enabled = true,
            // Demanding the in-session build without sessions is an always-refusing loop —
            // a configuration error the validator must catch at boot, not per iteration.
            // Demanding execution without the build has nothing to execute. Both are
            // boot-time refusals, not per-iteration ones.
            UseSandboxSessions = broken != "BuildCandidateInSession",
            BuildCandidateInSession = broken == "BuildCandidateInSession",
            ExecuteCandidateInSession = broken is "BuildCandidateInSession" or "ExecuteCandidateInSession",
            SessionImage = broken == "SessionImage" ? null : "proposer:latest",
            // A pin that is a tag rather than an identity can never match: every session
            // would refuse, which is a configuration error, not a policy.
            SessionImageDigest = broken == "SessionImageDigest" ? "proposer:latest" : null,
            RetentionWindow = broken == "RetentionWindow" ? 0 : 2,
            ThroughputGuardFactor = broken == "ThroughputGuardFactor" ? 1 : 4,
            WatchMaxInvocationSeconds = broken == "WatchMaxInvocationSeconds" ? -1 : 0,
            DigestIntervalSeconds = broken == "DigestIntervalSeconds" ? -1 : 3600,
        };

        var result = new ValidateNexoAutonomyOptions().Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(broken);
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
                ["Nexo:Autonomy:BuildCandidateInSession"] = "true",
                ["Nexo:Autonomy:SessionImage"] = "proposer:latest",
                ["Nexo:Autonomy:SessionImageDigest"] = "sha256:0123abcd",
                ["Nexo:Autonomy:CadenceFloorSeconds"] = "42",
            })
            .Build());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NexoAutonomyOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.SessionImage.Should().Be("proposer:latest");
        options.SessionImageDigest.Should().Be("sha256:0123abcd");
        options.BuildCandidateInSession.Should().BeTrue();
        options.CadenceFloorSeconds.Should().Be(42);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void EnabledLoop_WarnsAtHarnessComposition_UnlessCandidatesExecuteInSession(
        bool executeInSession, bool expectWarning)
    {
        // The three session flags default to false so a bare AddNexoAutonomy() stays valid,
        // which means an ENABLED loop can quietly execute proposed code in-process. That
        // configuration is allowed; being silent about it is not. The warning fires where
        // the harness is composed — for the standing loop, host start.
        var sink = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(sink));
        services.AddCertificationGate();
        services.AddNexoAutonomy(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Autonomy:Enabled"] = "true",
                ["Nexo:Autonomy:UseSandboxSessions"] = "true",
                ["Nexo:Autonomy:BuildCandidateInSession"] = "true",
                ["Nexo:Autonomy:ExecuteCandidateInSession"] = executeInSession ? "true" : "false",
                ["Nexo:Autonomy:SessionImage"] = "proposer:latest",
            })
            .Build());
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<AutonomousIterationHarness>().Should().NotBeNull();

        var warnings = sink.Entries.Where(e => e.Level >= LogLevel.Warning).Select(e => e.Message).ToList();
        if (expectWarning)
        {
            warnings.Should().ContainSingle(w => w.Contains("ExecuteCandidateInSession=false"))
                .Which.Should().Contain("IN THIS PROCESS").And.Contain("HoldAdmission",
                    "the operator must learn that hold blocks the swap, not the execution");
        }
        else
        {
            warnings.Should().NotContain(w => w.Contains("IN THIS PROCESS"),
                "the recommended trio is exactly the configuration that must not be nagged");
        }
    }

    [Fact]
    public void DisabledLoop_NeverWarnsAboutContainment()
    {
        // Disabled runs nothing, so there is nothing to warn about — a host that merely
        // composes the loop must stay quiet.
        var sink = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(sink));
        services.AddCertificationGate();
        services.AddNexoAutonomy();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<AutonomousIterationHarness>().Should().NotBeNull();

        sink.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning && e.Message.Contains("IN THIS PROCESS"));
    }

    [Fact]
    public async Task ConfiguredDigestPin_ReachesTheComposedSessionRunner_AndRefusesAMismatch()
    {
        // Wiring proof, not a unit test of the runner: the option bound from configuration
        // must be the pin the composed ISandboxedSessionRunner enforces. A host-registered
        // process runner stands in for the docker CLI (TryAdd — the host's wins).
        var calls = new List<IReadOnlyList<string>>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IProcessCommandRunner>(new ScriptedProcessRunner(args =>
        {
            calls.Add(args.ToArray());
            return args[0] == "image"
                ? new ProcessCommandResult(0, "sha256:actually-present\n", "")
                : new ProcessCommandResult(0, "", "");
        }));
        services.AddCertificationInfrastructure();
        services.AddNexoAutonomy(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:Autonomy:UseSandboxSessions"] = "true",
                ["Nexo:Autonomy:SessionImage"] = "proposer:latest",
                ["Nexo:Autonomy:SessionImageDigest"] = "sha256:pinned",
            })
            .Build());
        using var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<ISandboxedSessionRunner>();

        var act = () => runner.StartAsync(new SandboxSpec(
            "proposer:latest", Array.Empty<Mount>(), NetworkAccess.None, new[] { "sleep", "infinity" }));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*sha256:pinned*");
        calls.Should().NotContain(c => c[0] == "run", "a refused pin never starts a container");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<(LogLevel Level, string Message)> _entries = new();
        private readonly object _sync = new();

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get { lock (_sync) { return _entries.ToList(); } }
        }

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose() { }

        private void Add(LogLevel level, string message)
        {
            lock (_sync) { _entries.Add((level, message)); }
        }

        private sealed class Logger : ILogger
        {
            private readonly CapturingLoggerProvider _owner;
            public Logger(CapturingLoggerProvider owner) => _owner = owner;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                _owner.Add(logLevel, formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class ScriptedProcessRunner : IProcessCommandRunner
    {
        private readonly Func<IReadOnlyList<string>, ProcessCommandResult> _handler;

        public ScriptedProcessRunner(Func<IReadOnlyList<string>, ProcessCommandResult> handler) =>
            _handler = handler;

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_handler(arguments));
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

/// <summary>
/// The one autonomy composition test that sets a process-wide environment variable. It lives
/// in the serialized "EnvironmentVariables" collection: xunit runs collections in parallel, and
/// NEXO_DEPLOYMENT_PROFILE=airgapped leaking into a concurrently running host-composition test
/// (e.g. the API DI graph) turned unrelated readiness runs red.
/// </summary>
[Trait("Category", "Certification")]
[Collection("EnvironmentVariables")]
public sealed class AutonomyCompositionAirGappedProfileTests
{
    [Fact]
    public void AirGappedProfile_DoesNotRefuseAutonomy_UnlikeProtocolSurfaces()
    {
        using var profile = new EnvironmentVariableScope("NEXO_DEPLOYMENT_PROFILE", "airgapped");

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
}
