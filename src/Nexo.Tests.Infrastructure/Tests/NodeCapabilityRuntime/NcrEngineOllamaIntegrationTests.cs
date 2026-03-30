using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.Execution.Agentic;
using Nexo.Infrastructure.Execution;
using NodeCapabilityRuntimeImpl = Nexo.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public sealed class NcrEngineOllamaIntegrationTests
{
    [Fact]
    public async Task ResolveEnsureInference_RecordOutcome_RoundTrip_Works()
    {
        var handler = new FakeHttpMessageHandler((request, ct) =>
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
            Options.Create(new Nexo.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntimeOptions
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
                        MinRAMRequiredBytes = 1024L * 1024 * 1024,
                        Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
                        SupportedPlatforms = new HashSet<PlatformType> { PlatformType.Linux }
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
        var context = new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = "a1",
            BehaviorId = "b1",
            Provider = "ollama",
            Variables = new Dictionary<string, object>
            {
                ["nexo:user_facing"] = true
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
            new Nexo.Core.Application.Execution.Ports.BrickExecutionOutcome
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

    private sealed class TestBrick : Brick
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

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        public List<HttpRequestMessage> Requests { get; } = new();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return _handler(request, cancellationToken);
        }
    }
}
