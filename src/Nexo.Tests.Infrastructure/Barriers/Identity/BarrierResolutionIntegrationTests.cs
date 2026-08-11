using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Orchestration.Barriers;
using Nexo.Runtime.Barriers.Identity;
using Xunit;
using Nexo.Tests.Infrastructure.Helpers;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

/// <summary>Tests for barrier resolution integration.</summary>
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class BarrierResolutionIntegrationTests
{
    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task FullPipeline_PkiPriority_WinsOverJwt()
    {
        var hierarchy = CreateHierarchy();
        var pkiResult = new BarrierResolutionResult("internal", "PkiCertificate", BarrierAuthoritySource.PkiCertificate, "pki match");
        var jwtResult = new BarrierResolutionResult("confidential", "JwtClaim", BarrierAuthoritySource.JwtClaim, "jwt match");
        var pipeline = new DefaultBarrierIdentityResolverPipeline(
            [new StubResolver("PkiCertificate", pkiResult), new StubResolver("JwtClaim", jwtResult)],
            new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var resolved = await pipeline.ResolveAsync(CreateContext());

        resolved.Should().NotBeNull();
        resolved!.ResolvedLevel.Should().Be("internal");
        hierarchy.IsKnown(resolved.ResolvedLevel).Should().BeTrue();
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task RequireExplicitTrue_NoResolverMatch_ThrowsBarrierContextMissing()
    {
        var pipeline = new DefaultBarrierIdentityResolverPipeline(
            [new StubResolver("none", null)],
            new TestLogger<DefaultBarrierIdentityResolverPipeline>());
        var accessor = new CapturingAccessor();
        var audit = new CapturingAuditLog();

        var act = async () => await ResolveAtBoundaryAsync(
            pipeline,
            accessor,
            audit,
            CreateHierarchy(),
            new BarrierOptions { Levels = ["public", "internal"], RequireExplicitBarrier = true },
            CreateContext());

        await act.Should().ThrowAsync<BarrierContextMissingException>();
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task RequireExplicitFalse_NoResolverMatch_DefaultsToFloor()
    {
        var hierarchy = CreateHierarchy();
        var pipeline = new DefaultBarrierIdentityResolverPipeline(
            [new StubResolver("none", null)],
            new TestLogger<DefaultBarrierIdentityResolverPipeline>());
        var accessor = new CapturingAccessor();
        var audit = new CapturingAuditLog();

        var context = await ResolveAtBoundaryAsync(
            pipeline,
            accessor,
            audit,
            hierarchy,
            new BarrierOptions { Levels = ["public", "internal"], RequireExplicitBarrier = false },
            CreateContext());

        context.Level.Should().Be(hierarchy.Floor.Name);
        context.AuthoritySource.Should().Be(BarrierAuthoritySource.Default);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task IdentityResolved_AuditEvent_EmittedWithResolverNameAndDetail()
    {
        var hierarchy = CreateHierarchy();
        var pipeline = new DefaultBarrierIdentityResolverPipeline(
            [new StubResolver("JwtClaim", new BarrierResolutionResult("internal", "JwtClaim", BarrierAuthoritySource.JwtClaim, "claim mapped"))],
            new TestLogger<DefaultBarrierIdentityResolverPipeline>());
        var accessor = new CapturingAccessor();
        var audit = new CapturingAuditLog();

        await ResolveAtBoundaryAsync(
            pipeline,
            accessor,
            audit,
            hierarchy,
            new BarrierOptions { Levels = ["public", "internal"], RequireExplicitBarrier = true },
            CreateContext());

        audit.Events.Should().Contain(e =>
            e.EventType == BarrierAuditEventType.IdentityResolved &&
            e.Detail != null &&
            e.Detail.Contains("JwtClaim", StringComparison.OrdinalIgnoreCase) &&
            e.Detail.Contains("claim mapped", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<BarrierContext> ResolveAtBoundaryAsync(
        IBarrierIdentityResolverPipeline pipeline,
        IBarrierContextAccessor accessor,
        IBarrierAuditLog auditLog,
        BarrierHierarchy hierarchy,
        BarrierOptions options,
        BarrierResolutionContext resolutionContext)
    {
        var result = await pipeline.ResolveAsync(resolutionContext);
        if (result is null)
        {
            if (options.RequireExplicitBarrier)
                /// <summary>Barrier context missing exception.</summary>
                throw new BarrierContextMissingException("*", resolutionContext.CorrelationId);

            var defaultContext = BarrierContext.Create(
                hierarchy.Floor.Name,
                BarrierAuthoritySource.Default,
                "*",
                resolutionContext.CorrelationId,
                hierarchy,
                "Resolved by: DefaultFloor.");
            accessor.Initialize(defaultContext);
            await auditLog.RecordAsync(new BarrierAuditEvent(
                BarrierAuditEventType.DefaultApplied,
                defaultContext.Level,
                defaultContext.AuthoritySource,
                defaultContext.IssuedTo,
                defaultContext.CorrelationId,
                string.Empty,
                DateTimeOffset.UtcNow,
                "No explicit barrier or identity resolver match."));
            await auditLog.RecordAsync(new BarrierAuditEvent(
                BarrierAuditEventType.ContextInitialized,
                defaultContext.Level,
                defaultContext.AuthoritySource,
                defaultContext.IssuedTo,
                defaultContext.CorrelationId,
                string.Empty,
                DateTimeOffset.UtcNow));
            return defaultContext;
        }

        await auditLog.RecordAsync(new BarrierAuditEvent(
            BarrierAuditEventType.IdentityResolved,
            result.ResolvedLevel,
            result.AuthoritySource,
            "*",
            resolutionContext.CorrelationId,
            string.Empty,
            DateTimeOffset.UtcNow,
            $"Resolved by: {result.ResolverName}. {result.Detail}"));

        var barrierContext = BarrierContext.Create(
            result.ResolvedLevel,
            result.AuthoritySource,
            "*",
            resolutionContext.CorrelationId,
            hierarchy,
            string.IsNullOrWhiteSpace(result.Detail)
                ? $"Resolved by: {result.ResolverName}."
                : $"Resolved by: {result.ResolverName}. {result.Detail}");
        accessor.Initialize(barrierContext);
        await auditLog.RecordAsync(new BarrierAuditEvent(
            BarrierAuditEventType.ContextInitialized,
            barrierContext.Level,
            barrierContext.AuthoritySource,
            barrierContext.IssuedTo,
            barrierContext.CorrelationId,
            string.Empty,
            DateTimeOffset.UtcNow));
        return barrierContext;
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2)
        ]);

    private static BarrierResolutionContext CreateContext()
        => new(
            CorrelationId: "corr-1",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: "token",
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["subscription_tier"] = "pro"
            },
            ApiKey: null);

    /// <summary>Stub resolver.</summary>
    private sealed class StubResolver : IBarrierIdentityResolver
    {
        private readonly BarrierResolutionResult? _result;

        public StubResolver(string resolverName, BarrierResolutionResult? result)
        {
            ResolverName = resolverName;
            _result = result;
        }

        /// <summary>Resolver name.</summary>
        public string ResolverName { get; }

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => new(_result);
    }

    /// <summary>Capturing accessor.</summary>
    private sealed class CapturingAccessor : IBarrierContextAccessor
    {
        /// <summary>Current.</summary>
        public BarrierContext? Current { get; private set; }

        public void Initialize(BarrierContext context)
        {
            if (Current is not null)
                /// <summary>Invalid operation exception.</summary>
                /// <param name="initialized."">Initialized.".</param>
                throw new InvalidOperationException("Already initialized.");
            Current = context;
        }
    }

    /// <summary>Capturing audit log.</summary>
    private sealed class CapturingAuditLog : IBarrierAuditLog
    {
        /// <summary>Events.</summary>
        public List<BarrierAuditEvent> Events { get; } = [];

        public ValueTask RecordAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return default;
        }
    }
}
