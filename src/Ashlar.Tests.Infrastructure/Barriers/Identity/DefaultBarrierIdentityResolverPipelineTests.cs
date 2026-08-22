using FluentAssertions;
using Moq;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;
using Ashlar.Runtime.Barriers.Identity;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Barriers.Identity;

/// <summary>Tests for default barrier identity resolver pipeline.</summary>
public sealed class DefaultBarrierIdentityResolverPipelineTests
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
    public async Task ResolveAsync_ExplicitLevel_ReturnsImmediately_WithoutCallingResolvers()
    {
        var resolver = new Mock<IBarrierIdentityResolver>(MockBehavior.Strict);
        var logger = new TestLogger<DefaultBarrierIdentityResolverPipeline>();
        var sut = new DefaultBarrierIdentityResolverPipeline([resolver.Object], logger);

        var result = await sut.ResolveAsync(CreateContext(explicitLevel: "internal"));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
        result.ResolverName.Should().Be("Explicit");
        result.AuthoritySource.Should().Be(BarrierAuthoritySource.Cli);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_ExplicitLevelFromHeader_UsesHeaderAuthority()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());
        var context = new BarrierResolutionContext(
            CorrelationId: "corr-1",
            ExplicitLevel: "internal",
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-ashlar-barrier"] = "internal"
            },
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(),
            ApiKey: null);

        var result = await sut.ResolveAsync(context);

        result.Should().NotBeNull();
        result!.AuthoritySource.Should().Be(BarrierAuthoritySource.Header);
    }

    [Fact]
    public async Task ResolveAsync_FirstResolverMatches_SecondNotCalled()
    {
        var first = new Mock<IBarrierIdentityResolver>();
        first.SetupGet(x => x.ResolverName).Returns("first");
        first.Setup(x => x.TryResolveAsync(It.IsAny<BarrierResolutionContext>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<BarrierResolutionResult?>(new BarrierResolutionResult("internal", "first", BarrierAuthoritySource.JwtClaim)));

        var second = new Mock<IBarrierIdentityResolver>(MockBehavior.Strict);
        second.SetupGet(x => x.ResolverName).Returns("second");

        var sut = new DefaultBarrierIdentityResolverPipeline([first.Object, second.Object], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().NotBeNull();
        result!.ResolverName.Should().Be("first");
        second.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_FirstNull_SecondMatches_ReturnsSecond()
    {
        var first = new StubResolver("first", null);
        var second = new StubResolver("second", new BarrierResolutionResult("restricted", "second", BarrierAuthoritySource.ApiKey));
        var sut = new DefaultBarrierIdentityResolverPipeline([first, second], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().NotBeNull();
        result!.ResolverName.Should().Be("second");
    }

    [Fact]
    public async Task ResolveAsync_AllResolversNull_ReturnsNull()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline(
            [new StubResolver("first", null), new StubResolver("second", null)],
            new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_FirstThrows_SecondStillCalled_AndWarningLogged()
    {
        var first = new ThrowingResolver("first");
        var secondResult = new BarrierResolutionResult("public", "second", BarrierAuthoritySource.JwtClaim);
        var second = new StubResolver("second", secondResult);
        var logger = new TestLogger<DefaultBarrierIdentityResolverPipeline>();
        var sut = new DefaultBarrierIdentityResolverPipeline([first, second], logger);

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().Be(secondResult);
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("resolver failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ResolveAsync_EmptyResolverList_ReturnsNull()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhitespaceExplicitLevel_FallsThroughToResolvers()
    {
        var resolver = new StubResolver("stub", new BarrierResolutionResult("internal", "stub", BarrierAuthoritySource.ApiKey));
        var sut = new DefaultBarrierIdentityResolverPipeline([resolver], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext(explicitLevel: "   "));

        result.Should().NotBeNull();
        result!.ResolverName.Should().Be("stub");
    }

    [Fact]
    public async Task ResolveAsync_ExplicitLevelWithoutBarrierHeader_UsesCliAuthority()
    {
        var sut = new DefaultBarrierIdentityResolverPipeline([], new TestLogger<DefaultBarrierIdentityResolverPipeline>());

        var result = await sut.ResolveAsync(CreateContext(explicitLevel: "internal"));

        result.Should().NotBeNull();
        result!.AuthoritySource.Should().Be(BarrierAuthoritySource.Cli);
        result.Detail.Should().Contain("Explicit barrier provided");
    }

    [Fact]
    public async Task ResolveAsync_AllResolversThrow_ReturnsNullAndLogsWarnings()
    {
        var logger = new TestLogger<DefaultBarrierIdentityResolverPipeline>();
        var sut = new DefaultBarrierIdentityResolverPipeline(
            [new ThrowingResolver("first"), new ThrowingResolver("second")],
            logger);

        var result = await sut.ResolveAsync(CreateContext());

        result.Should().BeNull();
        logger.Entries.Count(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("resolver failed", StringComparison.OrdinalIgnoreCase)).Should().Be(2);
    }

    private static BarrierResolutionContext CreateContext(string? explicitLevel = null)
        => new(
            CorrelationId: "corr-1",
            ExplicitLevel: explicitLevel,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null);

    /// <summary>Stub resolver.</summary>
    private sealed class StubResolver : IBarrierIdentityResolver
    {
        private readonly BarrierResolutionResult? _result;

        public StubResolver(string name, BarrierResolutionResult? result)
        {
            ResolverName = name;
            _result = result;
        }

        /// <summary>Resolver name.</summary>
        public string ResolverName { get; }

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => new(_result);
    }

    /// <summary>Throwing resolver.</summary>
    private sealed class ThrowingResolver : IBarrierIdentityResolver
    {
        /// <summary>Throwing resolver.</summary>
        /// <param name="name">Name.</param>
        public ThrowingResolver(string name) => ResolverName = name;
        /// <summary>Resolver name.</summary>
        public string ResolverName { get; }

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("resolver failed");
    }
}
