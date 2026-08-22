using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Abstractions.Transport;
using Ashlar.Contracts;
using Ashlar.Orchestration.Architect;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Tests.Infrastructure.Helpers;
using Ashlar.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// The API host never registers a barrier middleware, so no request ever carries an explicit
/// barrier context. With the shipped appsettings the orchestrator must therefore default to
/// the floor level and actually run agents; when an operator opts back in to
/// <c>Ashlar:Barriers:RequireExplicitBarrier=true</c> the response must say WHY nothing ran
/// instead of a bare "0 agent(s) executed".
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class OrchestrationBarrierDefaultTests
{
    private sealed class SingleAgentArchitect : IArchitectAgent
    {
        public Task<DecompositionResult> DecomposeAsync(string request, CancellationToken cancellationToken = default)
            => DecomposeAsync(request, new DecompositionContext(), cancellationToken);

        public Task<DecompositionResult> DecomposeAsync(
            string request,
            DecompositionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new DecompositionResult
            {
                OriginalRequest = request,
                Agents = [new AgentSpawnSpec { AgentId = "stub-agent", Domain = "General", Goal = "Echo" }],
            });
    }

    private sealed class EchoAgentTransport : IAgentTransport
    {
        public Task<AgentResult> SendAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(
                Success: true,
                Output: new Dictionary<string, object?> { ["echoed"] = request.AgentName },
                CorrelationId: request.CorrelationId));

        public Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TransportHealth(true, "echo"));
    }

    private static WebApplicationFactory<Program> CreateFactory(IDictionary<string, string?>? settings = null)
        => new AshlarApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            if (settings is not null)
            {
                foreach (var pair in settings)
                {
                    builder.UseSetting(pair.Key, pair.Value);
                }
            }

            builder.ConfigureServices(services =>
            {
                // Later registrations win for plain resolutions: the stub architect replaces the
                // LLM-backed one and the echo transport replaces the kernel's routing transport,
                // so the only thing left between the request and "1 agent(s) executed" is the
                // barrier guard.
                services.AddSingleton<IArchitectAgent, SingleAgentArchitect>();
                services.AddSingleton<IAgentTransport, EchoAgentTransport>();
            });
        });

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Copilot_task_with_shipped_appsettings_executes_the_agent()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/copilot/task", new { task = "Echo something" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("summary").GetString().Should().Be("1 agent(s) executed");
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Orchestrate_with_shipped_appsettings_executes_the_agent()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/orchestrate", new OrchestrationRequest("Echo something"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<OrchestrationResponse>();
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.Summary.Should().Be("1 agent(s) executed");
        dto.ErrorCode.Should().BeNull();
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Orchestrate_with_RequireExplicitBarrier_opt_in_reports_why_nothing_ran()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Ashlar:Barriers:RequireExplicitBarrier"] = "true",
        });
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/orchestrate", new OrchestrationRequest("Echo something"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<OrchestrationResponse>();
        dto.Should().NotBeNull();
        dto!.Success.Should().BeFalse();
        dto.ErrorCode.Should().Be("BARRIER_CONTEXT_MISSING");
        dto.Summary.Should().StartWith("0 agent(s) executed: ")
            .And.Contain("Barrier context is required but missing")
            .And.Contain("RequireExplicitBarrier");
        dto.Escalations.Should().BeGreaterThan(0);
    }
}
