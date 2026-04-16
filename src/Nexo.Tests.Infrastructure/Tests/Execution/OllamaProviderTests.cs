using System.Net;
using System.Text;
using FluentAssertions;
using Nexo.Infrastructure.Execution.Ollama;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class OllamaProviderTests
{
    [Fact]
    public void Constructor_LoadsManifest_FromTagsEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""
        {
          "models": [
            { "name": "llama3.2:3b", "size": 1234, "modified_at": "2026-01-05T12:00:00Z" },
            { "name": "llava:7b", "size": 9876, "modified_at": "2026-01-06T12:00:00Z" }
          ]
        }
        """));

        using var httpClient = new HttpClient(handler);
        var sut = new OllamaProvider(httpClient, "http://localhost:11434");

        sut.IsAvailable.Should().BeTrue();
        sut.Manifest.Select(model => model.Name).Should().Contain(new[] { "llama3.2:3b", "llava:7b" });
        sut.Manifest.Single(model => model.Name == "llama3.2:3b").Size.Should().Be(1234);
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/api/tags");
    }

    [Fact]
    public async Task RefreshModelsAsync_UpdatesCachedManifest()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse("""{ "models": [ { "name": "llama3.2:3b", "size": 1234 } ] }"""),
            JsonResponse("""{ "models": [ { "name": "qwen2.5:7b", "size": 4321 } ] }""")
        });

        var handler = new FakeHttpMessageHandler(_ => responses.Dequeue());
        using var httpClient = new HttpClient(handler);
        var sut = new OllamaProvider(httpClient, "http://localhost:11434");

        var refreshResult = await sut.RefreshModelsAsync();

        refreshResult.IsSuccess.Should().BeTrue();
        sut.Manifest.Select(model => model.Name).Should().ContainSingle().Which.Should().Be("qwen2.5:7b");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateModel_ResolvesBareFamilyName_ToLatestTag()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""
        {
          "models": [
            { "name": "llama3.1:latest", "size": 1234 }
          ]
        }
        """));
        using var httpClient = new HttpClient(handler);
        var sut = new OllamaProvider(httpClient, "http://localhost:11434");

        var validation = sut.ValidateModel("llama3.1");

        validation.IsSuccess.Should().BeTrue();
        validation.Value.Should().NotBeNull();
        validation.Value!.Name.Should().Be("llama3.1:latest");
    }

    [Fact]
    public void ValidateModel_WhenMissing_ReturnsStructuredResultError()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("""
        {
          "models": [
            { "name": "llama3.2:3b", "size": 1234 }
          ]
        }
        """));
        using var httpClient = new HttpClient(handler);
        var sut = new OllamaProvider(httpClient, "http://localhost:11434");

        var validation = sut.ValidateModel("mistral:latest");

        validation.IsSuccess.Should().BeFalse();
        validation.Error.Should().NotBeNull();
        validation.Error!.Code.Should().Be("OLLAMA_MODEL_NOT_FOUND");
        validation.Error.Metadata.Should().NotBeNull();
        validation.Error.Metadata!["requestedModel"].Should().Be("mistral:latest");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnreachable_MarksProviderUnavailable()
    {
        var requestCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return JsonResponse("""{ "models": [ { "name": "llama3.2:3b", "size": 1234 } ] }""");
            }

            throw new HttpRequestException("Connection refused");
        });

        using var httpClient = new HttpClient(handler);
        var sut = new OllamaProvider(httpClient, "http://localhost:11434");

        var health = await sut.CheckHealthAsync();

        health.IsSuccess.Should().BeFalse();
        health.Error.Should().NotBeNull();
        health.Error!.Code.Should().Be("OLLAMA_UNREACHABLE");
        sut.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteChatAsync_WhenModelMissing_DoesNotCallChatEndpoint()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/tags");
            return JsonResponse("""
            {
              "models": [
                { "name": "llama3.2:3b", "size": 1234 }
              ]
            }
            """);
        });

        using var httpClient = new HttpClient(handler);
        var sut = new OllamaProvider(httpClient, "http://localhost:11434");

        var result = await sut.ExecuteChatAsync("gemma3:4b", "system", "user", null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("OLLAMA_MODEL_NOT_FOUND");
        handler.Requests.Should().ContainSingle(request => request.RequestUri!.AbsolutePath == "/api/tags");
        handler.Requests.Should().NotContain(request => request.RequestUri!.AbsolutePath == "/api/chat");
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public List<HttpRequestMessage> Requests { get; } = new();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_handler(request));
        }
    }
}
