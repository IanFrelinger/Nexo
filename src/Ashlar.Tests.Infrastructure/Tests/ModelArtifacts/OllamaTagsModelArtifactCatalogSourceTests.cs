using Ashlar.Agents.TestKit;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Ashlar.Infrastructure.ModelArtifacts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.ModelArtifacts;

/// <summary>Tests for ollama tags model artifact catalog source.</summary>
public sealed class OllamaTagsModelArtifactCatalogSourceTests
{
    [Fact]
    public async Task ListAsync_ParsesTagsResponse()
    {
        var handler = StubHttpMessageHandler.ForPath("/api/tags", 
            """{"models":[{"name":"phi3:mini","size":2048,"modified_at":"2024-01-02T00:00:00Z"}]}""");

        await using var provider = new ServiceCollection()
            .AddHttpClient(OllamaTagsModelArtifactCatalogSource.HttpClientName)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost/", UriKind.Absolute))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .Services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var sut = new OllamaTagsModelArtifactCatalogSource(factory);

        (await sut.IsAvailableAsync()).Should().BeTrue();

        var list = await sut.ListAsync();
        list.Should().ContainSingle();
        list[0].Id.Should().Be("phi3:mini");
        list[0].SourceId.Should().Be("ollama-tags");
        list[0].SizeHintBytes.Should().Be(2048);
    }

    /// <summary>Tests for tags json handler.</summary>
}
