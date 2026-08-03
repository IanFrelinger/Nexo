using Nexo.Agents.TestKit;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for ollama model serving backend gap coverage.</summary>
public sealed class OllamaModelServingBackendGapCoverageTests
{
    [Fact]
    public void BackendType_is_ollama()
    {
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) => Task.FromResult(Ok("{}"))))
        {
            BaseAddress = new Uri("http://127.0.0.1:11434/"),
        };

        new OllamaModelServingBackend(client, Options.Create(new OllamaBackendOptions()))
            .BackendType.Should().Be(BackendType.Ollama);
    }

    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var options = Options.Create(new OllamaBackendOptions());
        var actClient = () => new OllamaModelServingBackend(null!, options);
        var actOptions = () => new OllamaModelServingBackend(new HttpClient(), null!);

        actClient.Should().Throw<ArgumentNullException>();
        actOptions.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task IsAvailableAsync_records_metrics_for_success_and_failure()
    {
        var metrics = new RecordingMetrics();
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            return req.RequestUri!.AbsolutePath == "/api/tags"
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
                : Task.FromResult(Ok("{}"));
        });

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:11434/") };
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions()),
            metrics);

        (await sut.IsAvailableAsync()).Should().BeFalse();
        metrics.Counters.Should().ContainKey("ncr.ollama.tags.failure");
        metrics.ExecutionTimes.Should().ContainKey("ncr.ollama.tags.duration");
    }

    [Fact]
    public async Task IsAvailableAsync_records_error_metrics_when_request_throws()
    {
        var metrics = new RecordingMetrics();
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("down")))
        {
            BaseAddress = new Uri("http://127.0.0.1:11434/"),
        };
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions()),
            metrics);

        var act = () => sut.IsAvailableAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
        metrics.Counters.Should().ContainKey("ncr.ollama.tags.error");
    }

    [Fact]
    public async Task RunInferenceAsync_supports_system_prompt_parameters_and_json_escaping()
    {
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            capturedBody = req.Content!.ReadAsStringAsync().Result;
            return Task.FromResult(Ok("""
            { "message": { "content": "" } }
            """));
        });

        using var client = new HttpClient(handler);
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));

        var result = await sut.RunInferenceAsync(new InferenceRequest
        {
            ModelId = "phi3:mini",
            Prompt = "say \"hi\"\npath\\to\\file\u0001",
            SystemPrompt = "system\tprompt\b\f\r",
            Parameters = new Dictionary<string, object>
            {
                ["temperature"] = 0.7f,
                ["top_k"] = 40,
                ["stream"] = false,
                ["label"] = "fast",
                ["ratio"] = 0.25d,
                ["cost"] = 1.5m,
                ["nullable"] = null!,
            },
        });

        result.TokenCount.Should().Be(0);
        capturedBody.Should().Contain("\"temperature\":0.7");
        capturedBody.Should().Contain("\"top_k\":40");
        capturedBody.Should().Contain("\"stream\":false");
        capturedBody.Should().Contain("\"ratio\":0.25");
        capturedBody.Should().Contain("\"cost\":1.5");
        capturedBody.Should().Contain("\"nullable\":null");
        capturedBody.Should().Contain("\\\"hi\\\"");
        capturedBody.Should().Contain("\\n");
        capturedBody.Should().Contain("path\\\\to\\\\file");
        capturedBody.Should().Contain("\\u0001");
    }

    [Fact]
    public async Task RunInferenceAsync_rejects_missing_model_id()
    {
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) => Task.FromResult(Ok("{}"))));
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));

        var act = () => sut.RunInferenceAsync(new InferenceRequest { ModelId = "  " });
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task PullModelAsync_reports_progress_and_records_metrics()
    {
        var progressReports = new List<PullProgress>();
        var metrics = new RecordingMetrics();
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) => Task.FromResult(Ok("""{ "status":"success" }"""))));
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }),
            metrics);

        await sut.PullModelAsync("phi3:mini", new Progress<PullProgress>(progressReports.Add));

        progressReports.Should().ContainSingle(p => p.ModelId == "phi3:mini");
        metrics.Counters.Should().ContainKey("ncr.ollama.pull.success");
    }

    [Fact]
    public async Task ListLoadedModelsAsync_returns_empty_when_ps_fails_or_has_no_models()
    {
        var handler = new StubHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath switch
        {
            "/api/ps" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            _ => Task.FromResult(Ok("{}")),
        });

        using var client = new HttpClient(handler);
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));

        (await sut.ListLoadedModelsAsync()).Should().BeEmpty();

        handler.SetResponder((req, _) => req.RequestUri!.AbsolutePath == "/api/ps"
            ? Task.FromResult(Ok("""{ "models": [] }"""))
            : Task.FromResult(Ok("{}")));

        (await sut.ListLoadedModelsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ListLoadedModelsAsync_returns_model_names_from_ps_response()
    {
        using var client = new HttpClient(new StubHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath == "/api/ps"
            ? Task.FromResult(Ok("""
            {
              "models": [
                { "name": "phi3:mini" },
                { "name": " " },
                { "size": 1 }
              ]
            }
            """))
            : Task.FromResult(Ok("{}"))));
        var metrics = new RecordingMetrics();
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }),
            metrics);

        var models = await sut.ListLoadedModelsAsync();

        models.Should().Equal("phi3:mini");
        metrics.Counters.Should().ContainKey("ncr.ollama.ps.success");
    }

    [Fact]
    public async Task ListLoadedModelsAsync_records_error_metrics_when_request_throws()
    {
        var metrics = new RecordingMetrics();
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("down")));
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }),
            metrics);

        var act = () => sut.ListLoadedModelsAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
        metrics.Counters.Should().ContainKey("ncr.ollama.ps.error");
    }

    [Fact]
    public async Task PullModelAsync_records_error_metrics_when_pull_fails()
    {
        var metrics = new RecordingMetrics();
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))));
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }),
            metrics);

        var act = () => sut.PullModelAsync("phi3:mini");
        await act.Should().ThrowAsync<HttpRequestException>();
        metrics.Counters.Should().ContainKey("ncr.ollama.pull.error");
    }

    [Fact]
    public async Task LoadUnload_and_inference_record_error_metrics_on_http_failures()
    {
        var metrics = new RecordingMetrics();
        using var client = new HttpClient(new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))));
        var sut = new OllamaModelServingBackend(
            client,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }),
            metrics);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.LoadModelAsync("phi3:mini"));
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.UnloadModelAsync("phi3:mini"));
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.RunInferenceAsync(new InferenceRequest
        {
            ModelId = "phi3:mini",
            Prompt = "hello",
        }));

        metrics.Counters.Should().ContainKey("ncr.ollama.generate.error");
        metrics.Counters.Should().ContainKey("ncr.ollama.chat.error");
    }

    /// <summary>Ok.</summary>
    /// <param name="json">Json.</param>
    private static HttpResponseMessage Ok(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Tests for fake handler.</summary>

    /// <summary>Tests for recording metrics.</summary>
    private sealed class RecordingMetrics : IMetricsCollector
    {
        /// <summary>Counters.</summary>
        public Dictionary<string, long> Counters { get; } = new(StringComparer.Ordinal);
        /// <summary>Execution times.</summary>
        public Dictionary<string, TimeSpan> ExecutionTimes { get; } = new(StringComparer.Ordinal);

        public void IncrementCounter(string counterName, int value = 1)
            => Counters[counterName] = Counters.GetValueOrDefault(counterName) + value;

        public void RecordExecutionTime(string operationName, TimeSpan duration)
            => ExecutionTimes[operationName] = duration;

        public Task<MetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new MetricsSnapshot
            {
                Counters = new Dictionary<string, long>(Counters),
                ExecutionTimes = new Dictionary<string, TimeSpan>(ExecutionTimes),
            });
    }
}
