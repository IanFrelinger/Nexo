using FluentAssertions;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for open ai compatible endpoint.</summary>
public sealed class OpenAiCompatibleEndpointTests
{
    [Theory]
    [InlineData("http://localhost:8000", "http://localhost:8000/v1/chat/completions")]
    [InlineData("http://localhost:8000/v1", "http://localhost:8000/v1/chat/completions")]
    [InlineData("http://localhost:8000/v1/", "http://localhost:8000/v1/chat/completions")]
    [InlineData("http://host/v1/chat/completions", "http://host/v1/chat/completions")]
    public void NormalizeChatCompletionsUrl_AppendsOrPreservesV1Path(string input, string expected)
    {
        var uri = OpenAiCompatibleEndpoint.NormalizeChatCompletionsUrl(input);
        uri.ToString().Should().Be(expected);
    }

    [Fact]
    public void NormalizeChatCompletionsUrl_AddsHttpSchemeWhenMissing()
    {
        var uri = OpenAiCompatibleEndpoint.NormalizeChatCompletionsUrl("127.0.0.1:11435");
        uri.Scheme.Should().Be("http");
        uri.ToString().Should().Be("http://127.0.0.1:11435/v1/chat/completions");
    }
}
