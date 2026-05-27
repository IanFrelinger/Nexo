using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class JwtClaimBarrierResolverGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var options = new JwtClaimResolverOptions();
        var hierarchy = CreateHierarchy();
        var logger = new TestLogger<JwtClaimBarrierResolver>();

        var act = () => new JwtClaimBarrierResolver(null!, hierarchy, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");

        act = () => new JwtClaimBarrierResolver(options, null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchy");

        act = () => new JwtClaimBarrierResolver(options, hierarchy, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task TryResolveAsync_null_claim_name_in_options_returns_null()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = null!,
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "token",
            claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_whitespace_raw_jwt_returns_null()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "   ",
            claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_success_includes_claim_detail()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "token",
            claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().NotBeNull();
        result!.Detail.Should().Contain("tier=pro");
        result.ResolverName.Should().Be("JwtClaim");
    }

    [Fact]
    public async Task TryResolveAsync_unknown_mapped_level_logs_warning_and_returns_null()
    {
        var logger = new TestLogger<JwtClaimBarrierResolver>();
        var sut = new JwtClaimBarrierResolver(
            new JwtClaimResolverOptions
            {
                ClaimName = "tier",
                ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "missing" },
            },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "token",
            claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("unknown barrier level", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TryResolveAsync_missing_claim_value_returns_null()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "token",
            claims: new Dictionary<string, string>()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_null_context_returns_null()
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
    public async Task TryResolveAsync_unmapped_claim_value_returns_null()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "tier",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "token",
            claims: new Dictionary<string, string> { ["tier"] = "enterprise" }));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_whitespace_claim_name_returns_null()
    {
        var sut = CreateResolver(new JwtClaimResolverOptions
        {
            ClaimName = "   ",
            ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
        });

        var result = await sut.TryResolveAsync(CreateContext(
            rawJwt: "token",
            claims: new Dictionary<string, string> { ["tier"] = "pro" }));

        result.Should().BeNull();
    }

    private static JwtClaimBarrierResolver CreateResolver(JwtClaimResolverOptions options)
        => new(options, CreateHierarchy(), new TestLogger<JwtClaimBarrierResolver>());

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2),
        ]);

    private static BarrierResolutionContext CreateContext(
        string? rawJwt,
        IReadOnlyDictionary<string, string> claims)
        => new(
            CorrelationId: "corr-gap",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: rawJwt,
            JwtClaims: claims,
            ApiKey: null);
}
