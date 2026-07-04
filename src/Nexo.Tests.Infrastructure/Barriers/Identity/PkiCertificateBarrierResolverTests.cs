using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

/// <summary>Tests for pki certificate barrier resolver.</summary>
public sealed class PkiCertificateBarrierResolverTests
{
    [Fact]
    public async Task TryResolveAsync_SubjectExactMatch_ReturnsExpectedLevel()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "exact",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-a,O=Acme",
                    BarrierLevel = "internal"
                }
            ]
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_SubjectGlobMatch_ReturnsExpectedLevel()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "glob",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-*,O=Acme",
                    BarrierLevel = "internal"
                }
            ]
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-orders,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_SanMatch_ReturnsExpectedLevel()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "san",
                    MatchField = "SAN",
                    MatchPattern = "*restricted*.acme.internal",
                    BarrierLevel = "restricted"
                }
            ]
        });

        var result = await sut.TryResolveAsync(CreateContext(sans: ["api-restricted.acme.internal"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("restricted");
    }

    [Fact]
    public async Task TryResolveAsync_NoMatch_ReturnsNull()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "nomatch",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-*,O=Acme",
                    BarrierLevel = "internal"
                }
            ]
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=other,O=Acme"]));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_UnknownBarrierLevel_ReturnsNullAndLogsWarning()
    {
        var logger = new TestLogger<PkiCertificateBarrierResolver>();
        var sut = new PkiCertificateBarrierResolver(
            new PkiCertificateResolverOptions
            {
                Rules =
                [
                    new CertificateBarrierRule
                    {
                        Name = "invalid",
                        MatchField = "Subject",
                        MatchPattern = "CN=svc-*,O=Acme",
                        BarrierLevel = "does-not-exist"
                    }
                ]
            },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-orders,O=Acme"]));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("unknown barrier level", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TryResolveAsync_MultipleRules_FirstMatchWins()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "first",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-*,O=Acme",
                    BarrierLevel = "internal"
                },
                new CertificateBarrierRule
                {
                    Name = "second",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-orders,O=Acme",
                    BarrierLevel = "restricted"
                }
            ]
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-orders,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_NullContext_ReturnsNull()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions());

        var result = await sut.TryResolveAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_NullRule_IsSkipped()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules = [null!],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_UnknownMatchField_ReturnsNull()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "unknown-field",
                    MatchField = "Thumbprint",
                    MatchPattern = "*",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_SanMatch_UsesSanDetail()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "san-detail",
                    MatchField = "SAN",
                    MatchPattern = "api.internal",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(sans: ["api.internal"]));

        result.Should().NotBeNull();
        result!.Detail.Should().Contain("SAN 'api.internal'");
    }

    private static PkiCertificateBarrierResolver CreateResolver(PkiCertificateResolverOptions options)
        => new(options, CreateHierarchy(), new TestLogger<PkiCertificateBarrierResolver>());

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("restricted", 2)
        ]);

    private static BarrierResolutionContext CreateContext(
        IReadOnlyList<string>? subjects = null,
        IReadOnlyList<string>? sans = null)
        => new(
            CorrelationId: "corr-1",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: subjects ?? Array.Empty<string>(),
            CertSans: sans ?? Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null);
}
