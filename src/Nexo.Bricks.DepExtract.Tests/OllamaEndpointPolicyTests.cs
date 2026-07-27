using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class OllamaEndpointPolicyTests : IDisposable
{
    private readonly string? _allowRemote;
    private readonly string? _allowlist;
    private readonly string? _baseUrl;

    public OllamaEndpointPolicyTests()
    {
        _allowRemote = Environment.GetEnvironmentVariable(OllamaEndpointPolicy.AllowRemoteEnv);
        _allowlist = Environment.GetEnvironmentVariable(OllamaEndpointPolicy.AllowlistEnv);
        _baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowRemoteEnv, _allowRemote);
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowlistEnv, _allowlist);
        Environment.SetEnvironmentVariable("OLLAMA_BASE_URL", _baseUrl);
    }

    [Theory]
    [InlineData("http://127.0.0.1:11434")]
    [InlineData("http://localhost:11434")]
    [InlineData("http://[::1]:11434")]
    public void Loopback_is_allowed(string url)
    {
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowRemoteEnv, null);
        OllamaEndpointPolicy.IsAllowedEndpoint(url, out var reason).Should().BeTrue(reason);
        reason.Should().Contain("loopback");
    }

    [Fact]
    public void In_cluster_ollama_hostname_is_allowlisted_by_default()
    {
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowRemoteEnv, null);
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowlistEnv, null);
        OllamaEndpointPolicy.IsAllowedEndpoint("http://ollama:11434", out var reason).Should().BeTrue(reason);
        reason.Should().Contain("allowlist");
    }

    [Fact]
    public void Remote_host_is_rejected_unless_override()
    {
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowRemoteEnv, null);
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowlistEnv, "ollama");
        OllamaEndpointPolicy.IsAllowedEndpoint("http://evil.example:11434", out _).Should().BeFalse();

        var act = () => OllamaEndpointPolicy.AssertAirGappedOrThrow("http://evil.example:11434");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Air-gap*");
    }

    [Fact]
    public void Remote_allowed_when_override_set()
    {
        Environment.SetEnvironmentVariable(OllamaEndpointPolicy.AllowRemoteEnv, "1");
        var act = () => OllamaEndpointPolicy.AssertAirGappedOrThrow("http://evil.example:11434");
        act.Should().NotThrow();
    }
}
