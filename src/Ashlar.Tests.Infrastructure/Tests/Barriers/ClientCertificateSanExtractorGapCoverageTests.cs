#if NET8_0_OR_GREATER
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Ashlar.Runtime.Barriers.Identity;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for client certificate san extractor gap coverage.</summary>
public sealed class ClientCertificateSanExtractorGapCoverageTests
{
    [Fact]
    public void ExtractDnsSans_null_certificate_is_noop()
    {
        var sans = new List<string>();

        ClientCertificateSanExtractor.ExtractDnsSans((X509Certificate2?)null!, sans);

        sans.Should().BeEmpty();
    }

    [Fact]
    public void ExtractDnsSans_when_extensions_enumeration_throws_swallows_exception()
    {
        var sans = new List<string>();

        var act = () => ClientCertificateSanExtractor.ExtractDnsSans(new ThrowingExtensions(), sans);

        act.Should().NotThrow();
        sans.Should().BeEmpty();
    }

    /// <summary>Tests for throwing extensions.</summary>
    private sealed class ThrowingExtensions : IEnumerable<X509Extension>
    {
        public IEnumerator<X509Extension> GetEnumerator()
            => throw new InvalidOperationException("extensions failed");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
#endif
