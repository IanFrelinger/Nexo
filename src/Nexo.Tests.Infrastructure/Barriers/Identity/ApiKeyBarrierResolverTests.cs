using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

public sealed class ApiKeyBarrierResolverTests
{
    [Fact]
    public async Task TryResolveAsync_ValidHashedKeyMapping_ReturnsLevel()
    {
        var key = "test-key-123";
        var hash = ComputeSha256Hex(key);
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [hash] = "confidential"
            }
        });

        var result = await sut.TryResolveAsync(CreateContext(apiKey: key));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("confidential");
        result.AuthoritySource.Should().Be(BarrierAuthoritySource.ApiKey);
    }

    [Fact]
    public async Task TryResolveAsync_InvalidKey_ReturnsNull()
    {
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ComputeSha256Hex("expected-key")] = "internal"
            }
        });

        var result = await sut.TryResolveAsync(CreateContext(apiKey: "different-key"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_ComparisonUsesFixedTimeEquals_AndHandlesDifferentLengths()
    {
        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Nexo.Runtime/Barriers/Identity/Resolvers/ApiKeyBarrierResolver.cs"));

        File.ReadAllText(sourcePath).Should().Contain("CryptographicOperations.FixedTimeEquals");

        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ComputeSha256Hex("a")] = "public"
            }
        });

        var shortResult = await sut.TryResolveAsync(CreateContext(apiKey: "a"));
        var longResult = await sut.TryResolveAsync(CreateContext(apiKey: "very-long-key-value"));

        shortResult.Should().NotBeNull();
        longResult.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_DetailContainsOnlyPrefix_NeverFullKey()
    {
        var key = "test-key-123";
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ComputeSha256Hex(key)] = "internal"
            }
        });

        var result = await sut.TryResolveAsync(CreateContext(apiKey: key));

        result.Should().NotBeNull();
        result!.Detail.Should().Contain("test****");
        result.Detail.Should().NotContain(key);
    }

    [Fact]
    public async Task TryResolveAsync_MissingHeader_ReturnsNull()
    {
        var sut = CreateResolver(new ApiKeyResolverOptions
        {
            KeyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ComputeSha256Hex("abc")] = "internal"
            }
        });

        var result = await sut.TryResolveAsync(CreateContext(apiKey: null));

        result.Should().BeNull();
    }

    private static ApiKeyBarrierResolver CreateResolver(ApiKeyResolverOptions options)
        => new(options, CreateHierarchy(), new TestLogger<ApiKeyBarrierResolver>());

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2)
        ]);

    private static BarrierResolutionContext CreateContext(string? apiKey)
        => new(
            CorrelationId: "corr-1",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: apiKey);

    private static string ComputeSha256Hex(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
