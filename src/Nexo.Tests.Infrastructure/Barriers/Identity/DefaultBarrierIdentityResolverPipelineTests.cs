using FluentAssertions;
using Moq;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class DefaultBarrierIdentityResolverPipelineTests
{
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
                ["x-nexo-barrier"] = "internal"
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

    private sealed class StubResolver : IBarrierIdentityResolver
    {
        private readonly BarrierResolutionResult? _result;

        public StubResolver(string name, BarrierResolutionResult? result)
        {
            ResolverName = name;
            _result = result;
        }

        public string ResolverName { get; }

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => new(_result);
    }

    private sealed class ThrowingResolver : IBarrierIdentityResolver
    {
        public ThrowingResolver(string name) => ResolverName = name;
        public string ResolverName { get; }

        public ValueTask<BarrierResolutionResult?> TryResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }
}
