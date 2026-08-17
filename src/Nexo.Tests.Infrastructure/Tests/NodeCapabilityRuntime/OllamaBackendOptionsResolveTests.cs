using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Sdk.Extensions;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>
/// NCR Ollama base URL follows the same precedence as the MEAI model path:
/// NEXO_OLLAMA_BASE_URL env -> Nexo:NodeCapabilityRuntime:Ollama:BaseUrl -> legacy OLLAMA_BASE_URL env -> default.
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class OllamaBackendOptionsResolveTests
{
    private const string NexoEnv = "NEXO_OLLAMA_BASE_URL";
    private const string LegacyEnv = "OLLAMA_BASE_URL";

    [Fact]
    public void Nexo_env_wins_over_config_and_legacy_env()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(NexoEnv, "http://nexo-env:11434/");
            Environment.SetEnvironmentVariable(LegacyEnv, "http://legacy:11434");

            OllamaBackendOptions.ResolveBaseUrl("http://config:11434").Should().Be("http://nexo-env:11434");
        });
    }

    [Fact]
    public void Config_wins_over_legacy_env()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(LegacyEnv, "http://legacy:11434");

            OllamaBackendOptions.ResolveBaseUrl("http://config:11434/").Should().Be("http://config:11434");
        });
    }

    [Fact]
    public void Legacy_env_is_honoured_when_nothing_else_is_set()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(LegacyEnv, "http://ollama:11434");

            OllamaBackendOptions.ResolveBaseUrl(null).Should().Be("http://ollama:11434");
            OllamaBackendOptions.ResolveBaseUrl("  ").Should().Be("http://ollama:11434");
        });
    }

    [Fact]
    public void Default_applies_when_nothing_is_set()
    {
        WithCleanEnv(() =>
        {
            OllamaBackendOptions.ResolveBaseUrl(null).Should().Be(OllamaBackendOptions.DefaultBaseUrl);
        });
    }

    [Fact]
    public void Bound_options_fall_back_to_legacy_env_when_section_is_absent()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(LegacyEnv, "http://ollama:11434");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNodeCapabilityRuntimeCore(new ConfigurationBuilder().Build());

            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IOptions<OllamaBackendOptions>>().Value.BaseUrl
                .Should().Be("http://ollama:11434");
        });
    }

    private static void WithCleanEnv(Action body)
    {
        var previousNexo = Environment.GetEnvironmentVariable(NexoEnv);
        var previousLegacy = Environment.GetEnvironmentVariable(LegacyEnv);
        try
        {
            Environment.SetEnvironmentVariable(NexoEnv, null);
            Environment.SetEnvironmentVariable(LegacyEnv, null);
            body();
        }
        finally
        {
            Environment.SetEnvironmentVariable(NexoEnv, previousNexo);
            Environment.SetEnvironmentVariable(LegacyEnv, previousLegacy);
        }
    }
}
