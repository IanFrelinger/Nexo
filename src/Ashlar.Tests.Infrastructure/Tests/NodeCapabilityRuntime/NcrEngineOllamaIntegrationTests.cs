using Ashlar.Agents.TestKit;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Infrastructure.Execution.Agentic;
using Ashlar.Infrastructure.Execution;
using NodeCapabilityRuntimeImpl = Ashlar.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntime;
using Ashlar.Infrastructure.NodeCapabilityRuntime;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Backends;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Policies;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Profiles;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Scoring;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

[Trait("Category", "Integration")]
public sealed class NcrEngineOllamaIntegrationTests
{
    /// <summary>
    /// Model scoring uses the hardware profile OS (Windows/macOS/Linux), not the policy's nominal platform.
    /// </summary>
    private static readonly HashSet<PlatformType> UnitTestPlatforms = new()
    {
        PlatformType.Linux,
        PlatformType.Windows,
        PlatformType.macOS
    };

    [Fact]
    public async Task ResolveEnsureInference_RecordOutcome_RoundTrip_Works()
    {
        var handler = new StubHttpMessageHandler((request, ct) =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/chat" => Task.FromResult(JsonResponse("""
                {
                  "message": {
                    "content": "integration-ok"
                  }
                }
                """)),
                "/api/generate" => Task.FromResult(JsonResponse("""{ "response":"ok" }""")),
                "/api/ps" => Task.FromResult(JsonResponse("""
                {
                  "models": [
                    { "name": "phi3-mini" }
                  ]
                }
                """)),
                "/api/tags" => Task.FromResult(JsonResponse("""{ "models": [ { "name":"phi3-mini" } ] }""")),
                _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
            };
        });

        using var httpClient = new HttpClient(handler);
        var backend = new OllamaModelServingBackend(
            httpClient,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));
        var lifecycle = new DefaultModelLifecycleManager(backend);
        var policy = new LinuxPolicy();
        var runtime = new NodeCapabilityRuntimeImpl(
            new EnvironmentHardwareProfiler(),
            policy,
            lifecycle,
            new ModelScoringService(policy),
            Options.Create(new Ashlar.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntimeOptions
            {
                NodeId = "integration-node",
                DefaultModels =
                [
                    new ModelDescriptor
                    {
                        Id = "phi3-mini",
                        Provider = "ollama",
                        ProviderModelId = "phi3-mini",
                        State = ModelState.Warm,
                        QualityScore = 0.8f,
                        SpeedScore = 0.8f,
                        MinRAMRequiredBytes = 0,
                        Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
                        SupportedPlatforms = UnitTestPlatforms
                    }
                ]
            }),
            NullLogger<NodeCapabilityRuntimeImpl>.Instance);

        var engine = new NcrAgenticBrickEngine(runtime);
        var brick = new TestBrick
        {
            Id = "gen",
            Name = "gen",
            Category = BrickCategory.Generation,
            Description = "generation",
            Implementations = new BrickImplementations
            {
                Agentic = new AgenticImplementation { Id = "a", Name = "a", Description = "a" }
            }
        };
        var context = new Ashlar.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "a1",
            BehaviorId = "b1",
            Provider = "ollama",
            Variables = new Dictionary<string, object>
            {
                ["ashlar:user_facing"] = true
            }
        };

        var resolution = await engine.ResolveModelForBrickAsync(brick, context, CancellationToken.None);
        var inference = await backend.RunInferenceAsync(new InferenceRequest
        {
            ModelId = resolution.Model.ProviderModelId ?? resolution.Model.Id,
            Prompt = "hello",
            SystemPrompt = "sys"
        });
        await engine.RecordExecutionOutcomeAsync(
            resolution,
            new Ashlar.Core.Application.Execution.Ports.BrickExecutionOutcome
            {
                Succeeded = true,
                Duration = TimeSpan.FromMilliseconds(50)
            },
            CancellationToken.None);

        resolution.Target.Should().Be(InferenceTarget.Local);
        inference.Output.Should().Be("integration-ok");
        var paths = handler.Requests.Select(r => r.RequestUri!.AbsolutePath).ToList();
        paths.Should().Contain("/api/ps");
        paths.Should().Contain("/api/chat");
    }

    [Fact]
    public async Task ResolveModel_WhenBackendUnavailable_Throws_AndRecordsFailureSignal()
    {
        var handler = new StubHttpMessageHandler((request, ct) =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/ps" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
                _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
            };
        });

        using var httpClient = new HttpClient(handler);
        var backend = new OllamaModelServingBackend(
            httpClient,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));
        var lifecycle = new DefaultModelLifecycleManager(backend);
        var policy = new LinuxPolicy();
        var runtime = new NodeCapabilityRuntimeImpl(
            new EnvironmentHardwareProfiler(),
            policy,
            lifecycle,
            new ModelScoringService(policy),
            Options.Create(new Ashlar.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntimeOptions
            {
                NodeId = "integration-node-failure",
                DefaultModels =
                [
                    new ModelDescriptor
                    {
                        Id = "phi3-mini",
                        Provider = "ollama",
                        ProviderModelId = "phi3-mini",
                        State = ModelState.Warm,
                        QualityScore = 0.8f,
                        SpeedScore = 0.8f,
                        MinRAMRequiredBytes = 0,
                        Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
                        SupportedPlatforms = UnitTestPlatforms
                    }
                ]
            }),
            NullLogger<NodeCapabilityRuntimeImpl>.Instance);

        var engine = new NcrAgenticBrickEngine(runtime);
        var brick = new TestBrick
        {
            Id = "gen-failure",
            Name = "gen-failure",
            Category = BrickCategory.Generation,
            Description = "generation",
            Implementations = new BrickImplementations
            {
                Agentic = new AgenticImplementation { Id = "a", Name = "a", Description = "a" }
            }
        };
        var context = new Ashlar.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "a1",
            BehaviorId = "b1",
            Provider = "ollama",
            Variables = new Dictionary<string, object>
            {
                ["ashlar:user_facing"] = true
            }
        };

        var act = async () => await engine.ResolveModelForBrickAsync(brick, context, CancellationToken.None);
        await act.Should().ThrowAsync<HttpRequestException>();
        handler.Requests.Select(r => r.RequestUri!.AbsolutePath).Should().Contain("/api/ps");
    }

    [Fact]
    public async Task RunInference_WhenSlowResponse_Completes_WithMeasuredDuration()
    {
        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/chat")
            {
                await Task.Delay(TimeSpan.FromMilliseconds(120), ct);
                return JsonResponse("""
                {
                  "message": {
                    "content": "slow-ok"
                  }
                }
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler);
        var backend = new OllamaModelServingBackend(
            httpClient,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));

        var inference = await backend.RunInferenceAsync(new InferenceRequest
        {
            ModelId = "phi3-mini",
            Prompt = "hello",
            SystemPrompt = "sys"
        });

        inference.Output.Should().Be("slow-ok");
        inference.Duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
        handler.Requests.Select(r => r.RequestUri!.AbsolutePath).Should().Contain("/api/chat");
    }

    [Fact]
    public async Task RunInference_WithIntermittentFailures_CompletesAcrossAttempts()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler((request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/chat")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            attempts++;
            if (attempts <= 2)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            return Task.FromResult(JsonResponse("""
            {
              "message": {
                "content": "eventual-ok"
              }
            }
            """));
        });

        using var httpClient = new HttpClient(handler);
        var backend = new OllamaModelServingBackend(
            httpClient,
            Options.Create(new OllamaBackendOptions { BaseUrl = "http://127.0.0.1:11434" }));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            backend.RunInferenceAsync(new InferenceRequest
            {
                ModelId = "phi3-mini",
                Prompt = "hello"
            }));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            backend.RunInferenceAsync(new InferenceRequest
            {
                ModelId = "phi3-mini",
                Prompt = "hello"
            }));

        var final = await backend.RunInferenceAsync(new InferenceRequest
        {
            ModelId = "phi3-mini",
            Prompt = "hello"
        });

        final.Output.Should().Be("eventual-ok");
        attempts.Should().Be(3);
    }

    [Ashlar.Tests.Infrastructure.Helpers.OptInFact(
        "ASHLAR_TEST_REAL_NCR_OLLAMA",
        "Live Ollama for NCR resolution + inference",
        Timeout = Ashlar.Tests.Infrastructure.Helpers.TestTimeouts.Integration)]
    public async Task LiveOllama_WhenEnabled_ResolvesAndInfers()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("ASHLAR_NODECAP_OLLAMA_URL")
                       ?? Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
                       ?? "http://127.0.0.1:11434").TrimEnd('/');
        var modelId = Environment.GetEnvironmentVariable("ASHLAR_TEST_REAL_NCR_MODEL")
                      ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL")
                      ?? "phi3:mini";

        using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        using var probeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            var tags = await probe.GetAsync($"{baseUrl}/api/tags", probeCts.Token);
            if (!tags.IsSuccessStatusCode)
                return;
        }
        catch (HttpRequestException)
        {
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl + "/", UriKind.Absolute) };
        var backend = new OllamaModelServingBackend(
            httpClient,
            Options.Create(new OllamaBackendOptions { BaseUrl = baseUrl }));
        var lifecycle = new DefaultModelLifecycleManager(backend);
        var policy = new LinuxPolicy();
        var runtime = new NodeCapabilityRuntimeImpl(
            new EnvironmentHardwareProfiler(),
            policy,
            lifecycle,
            new ModelScoringService(policy),
            Options.Create(new Ashlar.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntimeOptions
            {
                NodeId = "live-node",
                DefaultModels =
                [
                    new ModelDescriptor
                    {
                        Id = modelId,
                        Provider = "ollama",
                        ProviderModelId = modelId,
                        State = ModelState.Warm,
                        QualityScore = 0.8f,
                        SpeedScore = 0.7f,
                        MinRAMRequiredBytes = 1024L * 1024 * 1024,
                        Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
                        SupportedPlatforms = new HashSet<PlatformType> { PlatformType.Linux, PlatformType.macOS, PlatformType.Windows }
                    }
                ]
            }),
            NullLogger<NodeCapabilityRuntimeImpl>.Instance);

        var engine = new NcrAgenticBrickEngine(runtime);
        var brick = new TestBrick
        {
            Id = "live-gen",
            Name = "live-gen",
            Category = BrickCategory.Generation,
            Description = "live generation",
            Implementations = new BrickImplementations
            {
                Agentic = new AgenticImplementation { Id = "a", Name = "a", Description = "a" }
            }
        };
        var context = new Ashlar.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "agent-live",
            BehaviorId = "behavior-live",
            Provider = "ollama",
            Variables = new Dictionary<string, object> { ["ashlar:user_facing"] = true }
        };

        var resolution = await engine.ResolveModelForBrickAsync(brick, context, CancellationToken.None);
        var inference = await backend.RunInferenceAsync(new InferenceRequest
        {
            ModelId = resolution.Model.ProviderModelId ?? resolution.Model.Id,
            Prompt = "Reply with exactly: NCR_OLLAMA_OK",
            SystemPrompt = "Return concise output."
        });

        resolution.Target.Should().Be(InferenceTarget.Local);
        inference.Output.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class TestBrick : DomainBrick
    {
        public override Task<BrickOutput> ExecuteAsync(BrickInput input, ImplementationType implementation, IExecutionContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new BrickOutput());
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

}
