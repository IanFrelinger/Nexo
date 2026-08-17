using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Nexo.API.Endpoints;
using Nexo.Tests.Infrastructure.Helpers;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// ProdStyle coverage for the two probe routes the container HEALTHCHECKs, compose <c>--wait</c> and
/// the k8s sample rely on, on the real Nexo.API pipeline. <c>/health</c> is liveness (constant 200 while
/// the process serves HTTP); <c>/ready</c> is readiness and must answer 200 only between "host finished
/// starting" and "shutdown began" (<see cref="IHostApplicationLifetime"/>), otherwise 503 so an
/// orchestrator drains the endpoint before Kestrel closes. Both must stay outside the auth middleware
/// (probes carry no credentials). Configuration is injected with UseSetting only; no process environment
/// variables are touched.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class HealthAndReadinessProdStyleTests
{
    private static WebApplicationFactory<Program> CreateFactory(IDictionary<string, string?>? settings = null)
        => new NexoApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            if (settings is null)
            {
                return;
            }

            foreach (var pair in settings)
            {
                builder.UseSetting(pair.Key, pair.Value);
            }
        });

    private static async Task<(HttpStatusCode Status, string? State)> ProbeAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        return (response.StatusCode, json.RootElement.GetProperty("status").GetString());
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Health_and_ready_answer_200_once_the_host_has_started()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var health = await ProbeAsync(client, "/health");
        health.Status.Should().Be(HttpStatusCode.OK);
        health.State.Should().Be("healthy");

        // WebApplicationFactory starts the host before handing out a client, so ApplicationStarted has fired.
        var ready = await ProbeAsync(client, "/ready");
        ready.Status.Should().Be(HttpStatusCode.OK, "a started host with its DI graph built is ready to take traffic");
        ready.State.Should().Be("ready");
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public void Ready_is_503_while_starting_200_once_started_and_503_again_once_stopping()
    {
        // The in-process host cannot show the drain window: WebApplicationFactory runs the real Program.Main,
        // whose app.Run() waits on ApplicationStopping and tears the DI root down as soon as StopApplication()
        // fires. So the lifetime transitions are pinned on the decision itself, fed the same tokens the route
        // reads from IHostApplicationLifetime.
        using var started = new CancellationTokenSource();
        using var stopping = new CancellationTokenSource();

        NexoEndpoints.EvaluateReadiness(started.Token, stopping.Token)
            .Should().Be((StatusCodes.Status503ServiceUnavailable, "starting"), "before ApplicationStarted the DI graph / hosted services may still be coming up");

        started.Cancel();
        NexoEndpoints.EvaluateReadiness(started.Token, stopping.Token)
            .Should().Be((StatusCodes.Status200OK, "ready"));

        stopping.Cancel();
        NexoEndpoints.EvaluateReadiness(started.Token, stopping.Token)
            .Should().Be((StatusCodes.Status503ServiceUnavailable, "stopping"), "readiness must fail first so load balancers stop routing to a stopping instance, even though it started fine");
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Probes_are_reachable_without_credentials_when_built_in_auth_is_on()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Nexo:Security:AuthorizationMode"] = "ApiKey",
            ["Nexo:Security:ApiKey"] = "prodstyle-probe-key",
            ["Nexo:Security:AuthorizationScope"] = "AllApi",
        });
        using var client = factory.CreateClient();

        (await ProbeAsync(client, "/health")).Status.Should().Be(HttpStatusCode.OK, "HEALTHCHECK and livenessProbe send no headers");
        (await ProbeAsync(client, "/ready")).Status.Should().Be(HttpStatusCode.OK, "readinessProbe sends no headers either");
    }
}
