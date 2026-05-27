using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class ApiKeyBarrierResolverGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var options = new ApiKeyResolverOptions();
        var hierarchy = CreateHierarchy();
        var logger = new TestLogger<ApiKeyBarrierResolver>();

        var act = () => new ApiKeyBarrierResolver(null!, hierarchy, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");

        act = () => new ApiKeyBarrierResolver(options, null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchy");

        act = () => new ApiKeyBarrierResolver(options, hierarchy, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task TryResolveAsync_whitespace_api_key_returns_null()
    {
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>
            {
                [ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex("valid")] = "public",
            },
        });

        var result = await sut.TryResolveAsync(ApiKeyBarrierResolverTestsHelper.CreateContext("   "));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_uses_default_header_name_when_context_api_key_missing()
    {
        var key = "default-header-key";
        var hash = ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex(key);
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string> { [hash] = "internal" },
        });

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-nexo-api-key"] = key,
            },
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_null_context_returns_null()
    {
        var sut = CreateResolver(new ApiKeyResolverOptions());

        var result = await sut.TryResolveAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_reads_api_key_from_configured_header_name()
    {
        var key = "header-key";
        var hash = ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex(key);
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            HeaderName = "x-custom-api-key",
            KeyMapping = new Dictionary<string, string> { [hash] = "internal" },
        });

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-custom-api-key"] = key,
            },
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_unknown_mapped_level_logs_warning_and_returns_null()
    {
        var key = "abc";
        var hash = ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex(key);
        var logger = new TestLogger<ApiKeyBarrierResolver>();
        var sut = new ApiKeyBarrierResolver(
            new ApiKeyResolverOptions { KeyMapping = new Dictionary<string, string> { [hash] = "missing-level" } },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(ApiKeyBarrierResolverTestsHelper.CreateContext(key));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("unknown barrier level", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TryResolveAsync_short_key_uses_full_prefix_in_detail()
    {
        var key = "ab";
        var hash = ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex(key);
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string> { [hash] = "public" },
        });

        var result = await sut.TryResolveAsync(ApiKeyBarrierResolverTestsHelper.CreateContext(key));

        result.Should().NotBeNull();
        result!.Detail.Should().Contain("ab****");
    }

    [Fact]
    public async Task TryResolveAsync_no_matching_key_returns_null()
    {
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>
            {
                [ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex("configured")] = "public",
            },
        });

        var result = await sut.TryResolveAsync(ApiKeyBarrierResolverTestsHelper.CreateContext("other-key"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_skips_blank_configured_mapping_hash()
    {
        var key = "live-key";
        var hash = ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex(key);
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>
            {
                [""] = "internal",
                [hash] = "public",
            },
        });

        var result = await sut.TryResolveAsync(ApiKeyBarrierResolverTestsHelper.CreateContext(key));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("public");
    }

    [Fact]
    public async Task TryResolveAsync_prefers_context_api_key_over_header()
    {
        var contextKey = "context-key";
        var headerKey = "header-key";
        var hash = ApiKeyBarrierResolverTestsHelper.ComputeSha256Hex(contextKey);
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string> { [hash] = "internal" },
        });

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-nexo-api-key"] = headerKey,
            },
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: contextKey));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    private static ApiKeyBarrierResolver CreateResolver(ApiKeyResolverOptions options)
        => new(options, CreateHierarchy(), new TestLogger<ApiKeyBarrierResolver>());

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2),
        ]);
}

internal static class ApiKeyBarrierResolverTestsHelper
{
    public static BarrierResolutionContext CreateContext(string? apiKey)
        => new(
            CorrelationId: "corr-1",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: apiKey);

    public static string ComputeSha256Hex(string value)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        var sb = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
