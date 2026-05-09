using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.ModelArtifacts;
using Nexo.Infrastructure.ModelArtifacts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.ModelArtifacts;

public sealed class OllamaRemoteLibraryModelArtifactCatalogSourceTests
{
    [Fact]
    public async Task ListAsync_ParsesRemoteTagsResponse()
    {
        var handler = new TagsJsonHandler(
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
            .ConfigurePrimaryHttpMessageHandler(() => new TagsJsonHandler("""{"models":[]}"""));
        await using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var options = provider.GetRequiredService<IOptionsMonitor<OllamaRemoteLibraryCatalogOptions>>();
        var sut = new OllamaRemoteLibraryModelArtifactCatalogSource(factory, options);

        (await sut.IsAvailableAsync()).Should().BeFalse();
        (await sut.ListAsync()).Should().BeEmpty();
    }

    private sealed class TagsJsonHandler : HttpMessageHandler
    {
        private readonly string _json;

        public TagsJsonHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri is null ||
                !string.Equals(request.RequestUri.AbsolutePath.TrimEnd('/'), "/api/tags", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
