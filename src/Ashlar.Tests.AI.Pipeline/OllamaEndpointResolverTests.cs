using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ashlar.AI.Pipeline;
using Ashlar.AI.Pipeline.Clients;
using Xunit;

namespace Ashlar.Tests.AI.Pipeline;

/// <summary>
/// Precedence of the Ollama endpoint used by the default (MEAI) model path:
/// ASHLAR_OLLAMA_* env -> Ashlar:Meai config -> legacy OLLAMA_* env -> defaults.
/// Every compose stack sets only the legacy family, so the fallback is what stops the
/// containerised default path dialling its own localhost.
/// </summary>
public sealed class OllamaEndpointResolverTests
{
    private static readonly string[] EnvVars =
    {
        MeaiPipelineOptions.OllamaBaseUrlEnvVar,
        MeaiPipelineOptions.OllamaModelEnvVar,
        MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar,
        MeaiPipelineOptions.LegacyOllamaModelEnvVar,
    };

    [Fact]
    public void Ashlar_env_wins_over_config_and_legacy_env()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.OllamaBaseUrlEnvVar, "http://ashlar-env:11434/");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.OllamaModelEnvVar, " ashlar-env-model ");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar, "http://legacy:11434");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaModelEnvVar, "legacy-model");
            var options = new MeaiPipelineOptions { OllamaBaseUrl = "http://config:11434", OllamaModel = "config-model" };

            OllamaEndpointResolver.ResolveBaseUrl(options).Should().Be("http://ashlar-env:11434");
            OllamaEndpointResolver.ResolveModel(options).Should().Be("ashlar-env-model");
        });
    }

    [Fact]
    public void Config_wins_over_legacy_env()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar, "http://legacy:11434");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaModelEnvVar, "legacy-model");
            var options = new MeaiPipelineOptions { OllamaBaseUrl = "http://config:11434/", OllamaModel = "config-model" };

            OllamaEndpointResolver.ResolveBaseUrl(options).Should().Be("http://config:11434");
            OllamaEndpointResolver.ResolveModel(options).Should().Be("config-model");
        });
    }

    [Fact]
    public void Bound_Ashlar_Meai_section_wins_over_legacy_env()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar, "http://legacy:11434");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaModelEnvVar, "legacy-model");
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [MeaiPipelineOptions.SectionName + ":OllamaBaseUrl"] = "http://ollama-service:11434",
                    [MeaiPipelineOptions.SectionName + ":OllamaModel"] = "qwen2.5-coder:7b",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddAshlarMeaiPipeline(
                config,
                ollamaInnerFactory: _ => new FakeChatClient(),
                onnxInnerFactory: _ => new FakeChatClient());
            using ServiceProvider provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<MeaiPipelineOptions>>().Value;

            OllamaEndpointResolver.ResolveBaseUrl(options).Should().Be("http://ollama-service:11434");
            OllamaEndpointResolver.ResolveModel(options).Should().Be("qwen2.5-coder:7b");
        });
    }

    [Fact]
    public void Legacy_env_is_honoured_when_nothing_else_is_set()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar, "http://ollama:11434/");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaModelEnvVar, "legacy-model");
            var options = new MeaiPipelineOptions();

            OllamaEndpointResolver.ResolveBaseUrl(options).Should().Be("http://ollama:11434");
            OllamaEndpointResolver.ResolveModel(options).Should().Be("legacy-model");
        });
    }

    [Fact]
    public void Blank_values_do_not_mask_later_fallbacks()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.OllamaBaseUrlEnvVar, "   ");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar, "http://ollama:11434");
            var options = new MeaiPipelineOptions { OllamaBaseUrl = string.Empty };

            OllamaEndpointResolver.ResolveBaseUrl(options).Should().Be("http://ollama:11434");
        });
    }

    [Fact]
    public void Defaults_apply_when_nothing_is_set()
    {
        WithCleanEnv(() =>
        {
            OllamaEndpointResolver.ResolveBaseUrl(new MeaiPipelineOptions()).Should().Be("http://localhost:11434");
            OllamaEndpointResolver.ResolveModel(new MeaiPipelineOptions()).Should().Be("llama3.1:latest");
            OllamaEndpointResolver.ResolveBaseUrl(null).Should().Be(MeaiPipelineOptions.DefaultOllamaBaseUrl);
            OllamaEndpointResolver.ResolveModel(null).Should().Be(MeaiPipelineOptions.DefaultOllamaModel);
        });
    }

    [Fact]
    public void Http_chat_client_dials_the_resolved_endpoint()
    {
        WithCleanEnv(() =>
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar, "http://ollama:11434");
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaModelEnvVar, "legacy-model");

            using var client = new OllamaHttpChatClient(Options.Create(new MeaiPipelineOptions()));
            var metadata = client.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;

            metadata.Should().NotBeNull();
            metadata!.ProviderUri.Should().Be(new Uri("http://ollama:11434/"));
            metadata.DefaultModelId.Should().Be("legacy-model");
        });
    }

    /// <summary>Clears every Ollama env family for the duration of <paramref name="body"/>, then restores it.</summary>
    private static void WithCleanEnv(Action body)
    {
        var previous = EnvVars.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in EnvVars)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            body();
        }
        finally
        {
            foreach (var (name, value) in previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
