using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class DefaultBarrierIdentityResolverPipelineGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_logger()
    {
        var act = () => new DefaultBarrierIdentityResolverPipeline([], null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_treats_null_resolver_enumerable_as_empty()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline(null!, new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveAsync_throws_for_null_context()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var act = async () => await sut.ResolveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task ResolveAsync_whitespace_explicit_level_falls_through_to_resolvers()
    {
        var resolver = new StubResolver(
            "stub",
            new BarrierResolutionResult("internal", "stub", BarrierAuthoritySource.ApiKey));
        var sut = new DefaultBarrierIdentityResolverPipeline([resolver], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext(explicitLevel: "   "));

        result.Should().NotBeNull();
        result!.ResolverName.Should().Be("stub");
    }

    [Fact]
    public async Task ResolveAsync_explicit_level_without_barrier_header_uses_cli_authority()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr-gap",
            ExplicitLevel: "internal",
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().NotBeNull();
        result!.AuthoritySource.Should().Be(BarrierAuthoritySource.Cli);
    }

    [Fact]
    public async Task ResolveAsync_explicit_level_with_barrier_header_uses_header_authority()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr-gap",
            ExplicitLevel: "internal",
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-nexo-barrier"] = "internal",
            },
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().NotBeNull();
        result!.AuthoritySource.Should().Be(BarrierAuthoritySource.Header);
    }

    [Fact]
    public async Task ResolveAsync_all_resolvers_throw_returns_null_and_logs_warnings()
    {
        var logger = new TestLogger<DefaultBarrierIdentityResolverPipeline>();
        var sut = new DefaultBarrierIdentityResolverPipeline(
            [new ThrowingResolver("first"), new ThrowingResolver("second")],
            logger);

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("resolver failed", StringComparison.OrdinalIgnoreCase));
        logger.Entries.Count(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("resolver failed", StringComparison.OrdinalIgnoreCase)).Should().Be(2);
    }

    [Fact]
    public async Task ResolveAsync_explicit_level_short_circuits_before_resolvers()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline(
            [new ThrowingResolver("should-not-run")],
            new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext(explicitLevel: "public"));

        result.Should().NotBeNull();
        result!.ResolverName.Should().Be("Explicit");
        result.Detail.Should().Contain("Explicit barrier provided");
    }

    [Fact]
    public async Task ResolveAsync_no_resolvers_and_no_explicit_level_returns_null()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().BeNull();
    }

    private static BarrierResolutionContext CreateContext(string? explicitLevel = null)
        => new(
            CorrelationId: "corr-gap",
            ExplicitLevel: explicitLevel,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null);

    private sealed class StubResolver(string name, BarrierResolutionResult? result) : IBarrierIdentityResolver
    {
        public string ResolverName { get; } = name;

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => new(result);
    }

    private sealed class ThrowingResolver(string name) : IBarrierIdentityResolver
    {
        public string ResolverName { get; } = name;

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("resolver failed");
    }
}
