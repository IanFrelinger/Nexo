using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class JwtClaimBarrierResolverTests
{
    [Fact]
    public async Task TryResolveAsync_ClaimPresentAndMapped_ReturnsLevel()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "subscription_tier",
            ClaimValueMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pro"] = "internal"
            }
        });

        var result = await sut.TryResolveAsync(CreateContext(rawJwt: "token", claims: new Dictionary<string, string> { ["subscription_tier"] = "pro" }));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
        result.AuthoritySource.Should().Be(BarrierAuthoritySource.JwtClaim);
    }

    [Fact]
    public async Task TryResolveAsync_ClaimPresentButUnmapped_ReturnsNull()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["admin"] = "restricted" }
        });

        var result = await sut.TryResolveAsync(CreateContext(rawJwt: "token", claims: new Dictionary<string, string> { ["tier"] = "employee" }));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_ClaimAbsent_ReturnsNull()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" }
        });

        var result = await sut.TryResolveAsync(CreateContext(rawJwt: "token", claims: new Dictionary<string, string>()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_RawJwtMissing_ReturnsNull()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" }
        });

        var result = await sut.TryResolveAsync(CreateContext(rawJwt: null, claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_MappedLevelUnknown_ReturnsNullAndLogsWarning()
    {
        var logger = new TestLogger<JwtClaimBarrierResolver>();
        var sut = new JwtClaimBarrierResolver(
            new JwtClaimResolverOptions
            {
                ClaimName = "tier",
                ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "unknown-level" }
            },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(CreateContext(rawJwt: "token", claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("unknown barrier level", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TryResolveAsync_NullContext_ReturnsNull()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_EmptyClaimName_ReturnsNull()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "  ",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(rawJwt: "token", claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
    }

    private static JwtClaimBarrierResolver CreateResolver(JwtClaimResolverOptions options)
        => new(options, CreateHierarchy(), new TestLogger<JwtClaimBarrierResolver>());

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("restricted", 2)
        ]);

    private static BarrierResolutionContext CreateContext(string? rawJwt, IReadOnlyDictionary<string, string> claims)
        => new(
            CorrelationId: "corr-1",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: rawJwt,
            JwtClaims: claims,
            ApiKey: null);
}
