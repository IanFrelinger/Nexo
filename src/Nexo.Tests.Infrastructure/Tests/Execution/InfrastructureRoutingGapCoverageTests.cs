using Nexo.Agents.TestKit;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Ephemeral;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.Execution.Sdk.Extensions;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for infrastructure routing gap coverage.</summary>
public class InfrastructureRoutingGapCoverageTests
{
    [Fact]
    public async Task RunPodHttpClient_spinup_dispatch_poll_pull_and_terminate_succeed()
    {
        using var httpClient = CreateClient((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/v2/instances" && request.Method == HttpMethod.Post)
            {
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.OK, """{"instanceId":"inst-1"}""");
            }

            if (path == "/v2/instances/inst-1/jobs" && request.Method == HttpMethod.Post)
            {
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.OK, """{"jobId":"job-1"}""");
            }

            if (path == "/v2/jobs/job-1" && request.Method == HttpMethod.Get)
            {
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.OK, """{"status":"completed","message":"done"}""");
            }

            if (path == "/v2/jobs/job-1/result" && request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 }),
                };
            }

            if (path == "/v2/instances/inst-1" && request.Method == HttpMethod.Delete)
            {
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.OK, "{}");
            }

            /// <summary>Json.</summary>
            return Json(HttpStatusCode.NotFound, "{}");
        }, apiKey: "secret-key");

        var sut = CreateClient(httpClient);

        var spinUp = await sut.SpinUpInstance("model", "A100");
        spinUp.IsSuccess.Should().BeTrue();
        spinUp.Value!.InstanceId.Should().Be("inst-1");

        var dispatch = await sut.DispatchJob("inst-1", new RunPodJobPayload
        {
            ModelId = "model",
            Prompt = "hello",
            GenerationParameters = new Dictionary<string, object> { ["temperature"] = 0.5 },
            OutputFormat = "png",
            IsPriority = true,
        });
        dispatch.IsSuccess.Should().BeTrue();
        dispatch.Value!.JobId.Should().Be("job-1");

        var poll = await sut.PollJobStatus(dispatch.Value);
        poll.IsSuccess.Should().BeTrue();
        poll.Value!.State.Should().Be(RunPodJobState.Completed);

        var pull = await sut.PullResults(dispatch.Value);
        pull.IsSuccess.Should().BeTrue();
        pull.Value.Should().Equal(new byte[] { 1, 2, 3 });

        (await sut.TerminateInstance("inst-1")).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("queued", RunPodJobState.Queued)]
    [InlineData("running", RunPodJobState.Running)]
    [InlineData("failed", RunPodJobState.Failed)]
    [InlineData("weird", RunPodJobState.Unknown)]
    public async Task RunPodHttpClient_poll_maps_status_values(string raw, RunPodJobState expected)
    {
        using var httpClient = CreateClient((_, _) =>
            Json(HttpStatusCode.OK, $$"""{"status":"{{raw}}"}"""));

        var result = await CreateClient(httpClient).PollJobStatus(new JobHandle { JobId = "j1" });
        result.Value!.State.Should().Be(expected);
    }

    [Fact]
    public async Task RunPodHttpClient_returns_failures_for_http_errors_and_invalid_payloads()
    {
        using var httpClient = CreateClient((request, _) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/v2/instances")
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.BadRequest, "{}");

            if (request.Method == HttpMethod.Post)
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.OK, "{}");

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.Contains("/result"))
                /// <summary>Json.</summary>
                return Json(HttpStatusCode.InternalServerError, "{}");

            /// <summary>Json.</summary>
            return Json(HttpStatusCode.OK, "{}");
        });

        var sut = CreateClient(httpClient);

        (await sut.SpinUpInstance("m", "gpu")).IsSuccess.Should().BeFalse();
        (await sut.DispatchJob("i", new RunPodJobPayload { ModelId = "m", Prompt = "p" })).IsSuccess.Should().BeFalse();
        (await sut.PullResults(new JobHandle { JobId = "j" })).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunPodHttpClient_returns_failure_when_http_handler_throws()
    {
        using var httpClient = CreateClient((_, _) => throw new HttpRequestException("network down"));
        var sut = CreateClient(httpClient);

        (await sut.SpinUpInstance("m", "gpu")).Error!.Code.Should().Be("runpod.spinup_exception");
        (await sut.DispatchJob("i", new RunPodJobPayload { ModelId = "m", Prompt = "p" })).Error!.Code
            .Should().Be("runpod.dispatch_exception");
        (await sut.PollJobStatus(new JobHandle { JobId = "j" })).Error!.Code.Should().Be("runpod.poll_exception");
        (await sut.PullResults(new JobHandle { JobId = "j" })).Error!.Code.Should().Be("runpod.pull_exception");
        (await sut.TerminateInstance("i")).Error!.Code.Should().Be("runpod.terminate_exception");
    }

    [Fact]
    public void OllamaEphemeralLifecycle_get_base_url_requires_active_session()
    {
        var lifecycle = new OllamaEphemeralLifecycle(NullLogger<OllamaEphemeralLifecycle>.Instance);
        var act = () => lifecycle.GetBaseUrlAsync();
        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void AddNexoFederatedBrickMesh_throws_without_local_registry()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrickHostOptions.SectionName}:RemoteCatalogBaseUrls:0"] = "https://peer.example",
            })
            .Build();

        var act = () => services.AddNexoFederatedBrickMesh(config).BuildServiceProvider().GetRequiredService<IBrickRegistry>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*BrickRegistry*");
    }

    [Fact]
    public void AddNexoFederatedBrickMesh_returns_local_registry_without_remote_urls()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton(new BrickRegistry(Array.Empty<DomainBrick>()));
        var config = new ConfigurationBuilder().Build();

        services.AddNexoFederatedBrickMesh(config);
        var registry = services.BuildServiceProvider().GetRequiredService<IBrickRegistry>();

        registry.Should().BeOfType<BrickRegistry>();
    }

    [Fact]
    public void AddNexoFederatedBrickMesh_rejects_invalid_remote_url()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton(new BrickRegistry(Array.Empty<DomainBrick>()));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrickHostOptions.SectionName}:RemoteCatalogBaseUrls:0"] = "not-a-url",
            })
            .Build();

        services.AddNexoFederatedBrickMesh(config);
        var act = () => services.BuildServiceProvider().GetRequiredService<IBrickRegistry>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*invalid URL*");
    }

    [Fact]
    public void AddNexoFederatedBrickMesh_builds_composite_registry_for_valid_remote_urls()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton(new BrickRegistry(Array.Empty<DomainBrick>()));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrickHostOptions.SectionName}:RemoteCatalogBaseUrls:0"] = "https://peer.example",
                [$"{BrickHostOptions.SectionName}:UseAuth"] = "true",
                [$"{BrickHostOptions.SectionName}:ApiKeyHeader"] = "X-Test-Key",
                [$"{BrickHostOptions.SectionName}:ApiKeyValue"] = "abc",
            })
            .Build();

        services.AddNexoFederatedBrickMesh(config);
        services.BuildServiceProvider().GetRequiredService<IBrickRegistry>()
            .Should().BeOfType<CompositeBrickRegistry>();
    }

    [Fact]
    public async Task CapabilityRoutingBrick_executes_from_brick_input_and_surfaces_delegate_failures()
    {
        var localExecutor = new StubRoutingLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [4, 5, 6],
            Provider = "local",
            ModelId = "m1",
            IsRemote = false,
        }));
        var router = new StubCapabilityRouter(new ExecutionTarget.Local(localExecutor, "local profile satisfied"));
        var brick = new CapabilityRoutingBrick(router, NullLogger<CapabilityRoutingBrick>.Instance);
        var input = new BrickInput();
        input.Set("modelId", "m1");
        input.Set("prompt", "draw");
        input.Set("computeClass", "High");
        input.Set("minimumVramBytes", 1024L);

        var success = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            RoutingTestExecutionContext(),
            CancellationToken.None);
        success.Get<bool>("success").Should().BeTrue();
        success.Get<string>("routedTarget").Should().Be("Local");

        var failingRouter = new StubCapabilityRouter(new ExecutionTarget.Local(
            new StubRoutingLocalExecutor(_ => Result<GenerationExecutionResult>.Failure("exec.fail", "boom")),
            "local forced"));
        var failingBrick = new CapabilityRoutingBrick(failingRouter, NullLogger<CapabilityRoutingBrick>.Instance);
        var failure = await failingBrick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            RoutingTestExecutionContext(),
            CancellationToken.None);
        failure.Get<bool>("success").Should().BeFalse();
        failure.Get<string>("errorCode").Should().Be("exec.fail");
    }

    /// <summary>Creates client.</summary>
    /// <param name="httpClient">Http client.</param>
    private static RunPodHttpClient CreateClient(HttpClient httpClient) =>
        new(
            httpClient,
            Options.Create(new RunPodBrickConfig { ApiKey = "key" }),
            NullLogger<RunPodHttpClient>.Instance);

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler,
        string? apiKey = null)
    {
        var client = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://api.runpod.io/"),
        };

        if (apiKey != null)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        return client;
    }

    /// <summary>Json.</summary>
    /// <param name="status">Status.</param>
    /// <param name="json">Json.</param>
    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Tests for fake http message handler.</summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

        /// <summary>Fake http message handler.</summary>
        /// <param name="handler">Handler.</param>
        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) =>
            _handler = handler;

        /// <summary>Send async.</summary>
        /// <param name="request">Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request, cancellationToken));
    }

    /// <summary>Tests for stub capability router.</summary>
    private sealed class StubCapabilityRouter(ExecutionTarget target) : ICapabilityRouter
    {
        /// <summary>Resolve execution target.</summary>
        /// <param name="requirements">Requirements.</param>
        public ExecutionTarget ResolveExecutionTarget(JobRequirements requirements) => target;
    }

    /// <summary>Tests for stub routing local executor.</summary>
    private sealed class StubRoutingLocalExecutor(Func<RunPodJobPayload, Result<GenerationExecutionResult>> handler) : ILocalExecutor
    {
        public Task<Result<GenerationExecutionResult>> ExecuteAsync(
            RunPodJobPayload payload,
            JobRequirements requirements,
            IExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(handler(payload));
    }

    /// <summary>Tests for routing test execution context.</summary>
    // Execution context for these tests. The IExecutionContext implementation now
    // lives in Nexo.Agents.TestKit.FakeExecutionContext; only the fixture values
    // that are specific to this suite stay here.
    private static FakeExecutionContext RoutingTestExecutionContext() => new()
    {
        AgentId = "routing-gap",
        BehaviorId = "generation",
        AuditMode = true,
        Provider = "local"
    };
}
