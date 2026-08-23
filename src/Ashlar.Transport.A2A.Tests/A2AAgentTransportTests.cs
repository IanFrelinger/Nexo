using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Transport;
using Xunit;

namespace Ashlar.Transport.A2A.Tests;

public sealed class A2AAgentTransportTests
{
    private static A2AAgentTransport Create(A2ATransportOptions? options = null)
        => new(
            Options.Create(options ?? new A2ATransportOptions()),
            NullLogger<A2AAgentTransport>.Instance);

    private static AgentInvocationRequest Request(string? endpoint) => new(
        AgentName: "agent",
        CorrelationId: "corr-1",
        Options: new AgentInvocationOptions(TimeSpan.FromSeconds(5), 0, endpoint));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://not-a2a.example.com")]
    [InlineData("a2a+ftp://bad-scheme")]
    public async Task Invalid_endpoints_produce_a_typed_failure_not_an_exception(string? endpoint)
    {
        var result = await Create().SendAsync(Request(endpoint));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("a2a.endpoint.invalid");
        result.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public async Task Transport_health_reports_ready()
    {
        var health = await Create().CheckHealthAsync();

        health.IsHealthy.Should().BeTrue();
        health.TransportName.Should().Be("a2a");
    }

    [Fact]
    public async Task Unreachable_endpoint_health_probe_reports_unhealthy()
    {
        var health = await Create().CheckEndpointAsync("a2a+http://127.0.0.1:1/api/a2a/ghost");

        health.IsHealthy.Should().BeFalse();
        health.TransportType.Should().Be("a2a");
    }

    [Fact]
    public void AddAshlarA2ATransport_registers_nothing_when_disabled()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddAshlarA2ATransport(configuration);

        services.Should().NotContain(d => d.ServiceType == typeof(A2AAgentTransport));
        services.Should().NotContain(d => d.ServiceType == typeof(AgentTransportSchemeRegistration));
    }

    [Fact]
    public void AddAshlarA2ATransport_contributes_the_scheme_registration_when_enabled()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{A2ATransportOptions.SectionPath}:Enabled"] = "true",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAshlarA2ATransport(configuration);
        using var provider = services.BuildServiceProvider();

        var registration = provider.GetRequiredService<AgentTransportSchemeRegistration>();
        registration.EndpointPrefix.Should().Be(A2AEndpointScheme.Prefix);
        registration.TransportFactory(provider).Should().BeOfType<A2AAgentTransport>();
    }
}
