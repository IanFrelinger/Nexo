using Ashlar.Agents.TestKit;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.ModelArtifacts;
using Ashlar.Infrastructure.ModelArtifacts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.ModelArtifacts;

/// <summary>Tests for ollama remote library model artifact catalog source.</summary>
public sealed class OllamaRemoteLibraryModelArtifactCatalogSourceTests
{
    [Fact]
    public async Task ListAsync_ParsesRemoteTagsResponse()
    {
        var handler = StubHttpMessageHandler.ForPath("/api/tags", 
            """{"models":[{"name":"llama3.2:latest","size":2048,"modified_at":"2024-01-02T00:00:00Z","digest":"abc"}]}""");

        var services = new ServiceCollection();
        services.AddOptions<OllamaRemoteLibraryCatalogOptions>().Configure(o =>
        {
            o.Enabled = true;
            o.BaseUrl = "https://example.test";
        });
        services.AddHttpClient(OllamaRemoteLibraryModelArtifactCatalogSource.HttpClientName)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://example.test/", UriKind.Absolute))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var options = provider.GetRequiredService<IOptionsMonitor<OllamaRemoteLibraryCatalogOptions>>();
        var sut = new OllamaRemoteLibraryModelArtifactCatalogSource(factory, options);

        (await sut.IsAvailableAsync()).Should().BeTrue();

        var list = await sut.ListAsync();
        list.Should().ContainSingle();
        list[0].Id.Should().Be("llama3.2:latest");
        list[0].Kind.Should().Be(ModelArtifactKind.OllamaRemoteLibraryModel);
        list[0].SourceId.Should().Be("ollama-remote-library");
        list[0].Metadata.Should().NotBeNull();
        list[0].Metadata!["digest"].Should().Be("abc");
    }

    [Fact]
    public async Task ListAsync_WhenDisabled_ReturnsEmpty()
    {
        var services = new ServiceCollection();
        services.AddOptions<OllamaRemoteLibraryCatalogOptions>().Configure(o => o.Enabled = false);
        services.AddHttpClient(OllamaRemoteLibraryModelArtifactCatalogSource.HttpClientName)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://example.test/", UriKind.Absolute))
            .ConfigurePrimaryHttpMessageHandler(() => StubHttpMessageHandler.ForPath("/api/tags", """{"models":[]}"""));
        await using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var options = provider.GetRequiredService<IOptionsMonitor<OllamaRemoteLibraryCatalogOptions>>();
        var sut = new OllamaRemoteLibraryModelArtifactCatalogSource(factory, options);

        (await sut.IsAvailableAsync()).Should().BeFalse();
        (await sut.ListAsync()).Should().BeEmpty();
    }

    /// <summary>Tests for tags json handler.</summary>
}
