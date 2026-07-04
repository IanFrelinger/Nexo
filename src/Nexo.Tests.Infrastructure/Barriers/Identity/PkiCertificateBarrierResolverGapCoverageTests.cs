using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

/// <summary>Tests for pki certificate barrier resolver gap coverage.</summary>
public sealed class PkiCertificateBarrierResolverGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var options = new PkiCertificateResolverOptions();
        var hierarchy = CreateHierarchy();
        var logger = new TestLogger<PkiCertificateBarrierResolver>();

        var act = () => new PkiCertificateBarrierResolver(null!, hierarchy, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");

        act = () => new PkiCertificateBarrierResolver(options, null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchy");

        act = () => new PkiCertificateBarrierResolver(options, hierarchy, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task TryResolveAsync_single_question_mark_wildcard_matches_one_character()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "single-char",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-?,O=Acme",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
        result.AuthoritySource.Should().Be(BarrierAuthoritySource.PkiCertificate);
    }

    [Fact]
    public async Task TryResolveAsync_subject_match_uses_subject_detail()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "subject-detail",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-a,O=Acme",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().NotBeNull();
        result!.Detail.Should().Contain("Subject 'CN=svc-a,O=Acme'");
    }

    [Fact]
    public async Task TryResolveAsync_whitespace_barrier_level_is_skipped_and_logs_warning()
    {
        var logger = new TestLogger<PkiCertificateBarrierResolver>();
        var sut = new PkiCertificateBarrierResolver(
            new PkiCertificateResolverOptions
            {
                Rules =
                [
                    new CertificateBarrierRule
                    {
                        Name = "blank-level",
                        MatchField = "Subject",
                        MatchPattern = "CN=svc-a,O=Acme",
                        BarrierLevel = "   ",
                    },
                ],
            },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("unknown barrier level", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TryResolveAsync_empty_subject_candidates_skip_rule()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "needs-subject",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-a,O=Acme",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: []));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_null_context_returns_null()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions());

        var result = await sut.TryResolveAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_san_match_uses_san_detail()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "san-rule",
                    MatchField = "SAN",
                    MatchPattern = "agent.local",
                    BarrierLevel = "confidential",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(
            subjects: ["CN=ignored"],
            sans: ["agent.local"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("confidential");
        result.Detail.Should().Contain("SAN 'agent.local'");
    }

    [Fact]
    public async Task TryResolveAsync_exact_match_without_wildcards()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "exact",
                    MatchField = "Subject",
                    MatchPattern = "CN=exact-match",
                    BarrierLevel = "public",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=exact-match"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("public");
    }

    [Fact]
    public async Task TryResolveAsync_asterisk_wildcard_matches_prefix()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "wildcard",
                    MatchField = "Subject",
                    MatchPattern = "CN=svc-*",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-prod-01,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_unknown_match_field_skips_rule()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules =
            [
                new CertificateBarrierRule
                {
                    Name = "bad-field",
                    MatchField = "Issuer",
                    MatchPattern = "CN=anything",
                    BarrierLevel = "internal",
                },
            ],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=anything"]));

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_null_rule_is_skipped()
    {
        var sut = CreateResolver(new PkiCertificateResolverOptions
        {
            Rules = [null!, new CertificateBarrierRule
            {
                Name = "valid",
                MatchField = "Subject",
                MatchPattern = "CN=svc-a,O=Acme",
                BarrierLevel = "internal",
            }],
        });

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("internal");
    }

    [Fact]
    public async Task TryResolveAsync_unknown_barrier_level_rule_is_skipped()
    {
        var logger = new TestLogger<PkiCertificateBarrierResolver>();
        var sut = new PkiCertificateBarrierResolver(
            new PkiCertificateResolverOptions
            {
                Rules =
                [
                    new CertificateBarrierRule
                    {
                        Name = "unknown-level",
                        MatchField = "Subject",
                        MatchPattern = "CN=svc-a,O=Acme",
                        BarrierLevel = "top-secret",
                    },
                    new CertificateBarrierRule
                    {
                        Name = "fallback",
                        MatchField = "Subject",
                        MatchPattern = "CN=svc-a,O=Acme",
                        BarrierLevel = "public",
                    },
                ],
            },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(CreateContext(subjects: ["CN=svc-a,O=Acme"]));

        result.Should().NotBeNull();
        result!.ResolvedLevel.Should().Be("public");
        logger.Entries.Should().Contain(entry =>
            entry.Message.Contains("unknown barrier level", StringComparison.OrdinalIgnoreCase));
    }

    private static PkiCertificateBarrierResolver CreateResolver(PkiCertificateResolverOptions options)
        => new(options, CreateHierarchy(), new TestLogger<PkiCertificateBarrierResolver>());

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2),
        ]);

    private static BarrierResolutionContext CreateContext(
        IReadOnlyList<string> subjects,
        IReadOnlyList<string>? sans = null)
        => new(
            CorrelationId: "corr-gap",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: subjects,
            CertSans: sans ?? Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null);
}
