using FluentAssertions;
using Xunit;

namespace Nexo.Ingress.AwsSns.Tests;

public sealed class SnsSmsMessageExtractorTests
{
    [Fact]
    public void Plain_text_body_passes_through()
    {
        var ok = SnsSmsMessageExtractor.TryExtract("  YES abc  ", out var from, out var body);
        ok.Should().BeTrue();
        from.Should().BeNull();
        body.Should().Be("YES abc");
    }

    [Fact]
    public void Json_message_extracts_origination_and_body()
    {
        const string raw = """{"originationNumber":"+15555550100","messageBody":"YES tok1"}""";
        var ok = SnsSmsMessageExtractor.TryExtract(raw, out var from, out var body);
        ok.Should().BeTrue();
        from.Should().Be("+15555550100");
        body.Should().Be("YES tok1");
    }
}
