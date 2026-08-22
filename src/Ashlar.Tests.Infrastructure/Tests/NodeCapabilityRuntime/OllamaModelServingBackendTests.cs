using Ashlar.Agents.TestKit;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Backends;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for ollama model serving backend.</summary>
public sealed class OllamaModelServingBackendTests
{
    [Fact]
    public async Task RunInferenceAsync_UsesChatEndpoint_AndParsesContent()
    {
        var handler = new StubHttpMessageHandler((request, ct) =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/chat");
            return Task.FromResult(JsonResponse("""
            {
              "message": {
                "content": "hello from ollama"
              }
            }
            """));
        });

        using var httpClient = new HttpClient(handler);
        var options = Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" });
        var sut = new OllamaModelServingBackend(httpClient, options);

        var result = await sut.RunInferenceAsync(new InferenceRequest
        {
            ModelId = "phi3:mini",
            Prompt = "Say hello"
        });

        result.Output.Should().Be("hello from ollama");
        handler.Requests.Count.Should().Be(1);
    }

    [Fact]
    public async Task PullLoadUnload_ListLoadedModels_UsesExpectedOllamaEndpoints()
    {
        var handler = new StubHttpMessageHandler((request, ct) =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/pull" => Task.FromResult(JsonResponse("""
                { "status":"success" }
                """)),
                "/api/generate" => Task.FromResult(JsonResponse("""
                { "response":"ok" }
                """)),
                "/api/ps" => Task.FromResult(JsonResponse("""
                {
                  "models": [
                    { "name": "phi3:mini" },
                    { "name": "nomic-embed-text" }
                  ]
                }
                """)),
                _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
            };
        });

        using var httpClient = new HttpClient(handler);
        var options = Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" });
        var sut = new OllamaModelServingBackend(httpClient, options);

        await sut.PullModelAsync("phi3:mini");
        await sut.LoadModelAsync("phi3:mini");
        await sut.UnloadModelAsync("phi3:mini");
        var loaded = await sut.ListLoadedModelsAsync();

        loaded.Should().Contain("phi3:mini");
        loaded.Should().Contain("nomic-embed-text");
        handler.Requests.Select(r => r.RequestUri!.AbsolutePath).Should().ContainInOrder(
            "/api/pull",
            "/api/generate",
            "/api/generate",
            "/api/ps");
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>Tests for fake http message handler.</summary>
}
