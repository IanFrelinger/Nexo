using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Barriers.Identity;

/// <summary>Tests for barrier resolver catch gap coverage.</summary>
public sealed class BarrierResolverCatchGapCoverageTests
{
    [Fact]
    public async Task ApiKeyResolver_logs_warning_when_header_lookup_throws()
    {
        var logger = new TestLogger<ApiKeyBarrierResolver>();
        var sut = new ApiKeyBarrierResolver(
            new ApiKeyResolverOptions(),
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new ThrowingHeaders(),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("API key barrier resolution failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task JwtClaimResolver_logs_warning_when_claim_lookup_throws()
    {
        var logger = new TestLogger<JwtClaimBarrierResolver>();
        var sut = new JwtClaimBarrierResolver(
            new JwtClaimResolverOptions
            {
                ClaimName = "tier",
                ClaimValueMapping = new Dictionary<string, string> { ["pro"] = "internal" },
            },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: Array.Empty<string>(),
            CertSans: Array.Empty<string>(),
            RawJwt: "token",
            JwtClaims: new ThrowingClaimDictionary(),
            ApiKey: null));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("JWT barrier resolution failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PkiCertificateResolver_logs_warning_when_rules_enumeration_throws()
    {
        var logger = new TestLogger<PkiCertificateBarrierResolver>();
        var sut = new PkiCertificateBarrierResolver(
            new PkiCertificateResolverOptions { Rules = new ThrowingRuleList() },
            CreateHierarchy(),
            logger);

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: ["CN=svc-a,O=Acme"],
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().BeNull();
        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("PKI barrier resolution failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PkiCertificateResolver_rejects_blank_pattern_or_candidate()
    {
        var sut = new PkiCertificateBarrierResolver(
            new PkiCertificateResolverOptions
            {
                Rules =
                [
                    new CertificateBarrierRule
                    {
                        Name = "blank-pattern",
                        MatchField = "Subject",
                        MatchPattern = "  ",
                        BarrierLevel = "internal",
                    },
                ],
            },
            CreateHierarchy(),
            new TestLogger<PkiCertificateBarrierResolver>());

        var result = await sut.TryResolveAsync(new BarrierResolutionContext(
            CorrelationId: "corr",
            ExplicitLevel: null,
            Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CertSubjects: ["CN=svc-a,O=Acme"],
            CertSans: Array.Empty<string>(),
            RawJwt: null,
            JwtClaims: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ApiKey: null));

        result.Should().BeNull();
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
            new BarrierLevel("confidential", 2),
        ]);

    /// <summary>Throwing headers.</summary>
    private sealed class ThrowingHeaders : IReadOnlyDictionary<string, string>
    {
        public string this[string key] => throw new InvalidOperationException("headers failed");
        public IEnumerable<string> Keys => throw new InvalidOperationException("headers failed");
        public IEnumerable<string> Values => throw new InvalidOperationException("headers failed");
        public int Count => throw new InvalidOperationException("headers failed");
        /// <summary>Contains key.</summary>
        /// <param name="key">Key.</param>
        public bool ContainsKey(string key) => throw new InvalidOperationException("headers failed");
        /// <summary>Gets enumerator.</summary>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new InvalidOperationException("headers failed");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>Attempts to get value; returns false on failure.</summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public bool TryGetValue(string key, out string value) => throw new InvalidOperationException("headers failed");
    }

    /// <summary>Throwing claim dictionary.</summary>
    private sealed class ThrowingClaimDictionary : IReadOnlyDictionary<string, string>
    {
        public string this[string key] => throw new InvalidOperationException("claims failed");
        public IEnumerable<string> Keys => throw new InvalidOperationException("claims failed");
        public IEnumerable<string> Values => throw new InvalidOperationException("claims failed");
        public int Count => throw new InvalidOperationException("claims failed");
        /// <summary>Contains key.</summary>
        /// <param name="key">Key.</param>
        public bool ContainsKey(string key) => throw new InvalidOperationException("claims failed");
        /// <summary>Gets enumerator.</summary>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new InvalidOperationException("claims failed");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>Attempts to get value; returns false on failure.</summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public bool TryGetValue(string key, out string value) => throw new InvalidOperationException("claims failed");
    }

    /// <summary>Throwing rule list.</summary>
    private sealed class ThrowingRuleList : IList<CertificateBarrierRule>
    {
        public CertificateBarrierRule this[int index]
        {
            get => throw new InvalidOperationException("rules failed");
            set => throw new InvalidOperationException("rules failed");
        }

        public int Count => throw new InvalidOperationException("rules failed");
        public bool IsReadOnly => false;
        /// <summary>Add.</summary>
        /// <param name="item">Item.</param>
        public void Add(CertificateBarrierRule item) => throw new InvalidOperationException("rules failed");
        /// <summary>Clear.</summary>
        public void Clear() => throw new InvalidOperationException("rules failed");
        /// <summary>Contains.</summary>
        /// <param name="item">Item.</param>
        public bool Contains(CertificateBarrierRule item) => throw new InvalidOperationException("rules failed");
        /// <summary>Copy to.</summary>
        /// <param name="array">Array.</param>
        /// <param name="arrayIndex">Array index.</param>
        public void CopyTo(CertificateBarrierRule[] array, int arrayIndex) => throw new InvalidOperationException("rules failed");
        /// <summary>Gets enumerator.</summary>
        public IEnumerator<CertificateBarrierRule> GetEnumerator() => throw new InvalidOperationException("rules failed");
        /// <summary>Index of.</summary>
        /// <param name="item">Item.</param>
        public int IndexOf(CertificateBarrierRule item) => throw new InvalidOperationException("rules failed");
        /// <summary>Insert.</summary>
        /// <param name="index">Index.</param>
        /// <param name="item">Item.</param>
        public void Insert(int index, CertificateBarrierRule item) => throw new InvalidOperationException("rules failed");
        /// <summary>Remove.</summary>
        /// <param name="item">Item.</param>
        public bool Remove(CertificateBarrierRule item) => throw new InvalidOperationException("rules failed");
        /// <summary>Remove at.</summary>
        /// <param name="index">Index.</param>
        public void RemoveAt(int index) => throw new InvalidOperationException("rules failed");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
