using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.AI.Pipeline;
using Nexo.AI.Pipeline.Clients;
using Xunit;

namespace Nexo.Tests.AI.Pipeline;

public sealed class MeaiPipelineRegistrationTests
{
    [Fact]
    public void IsMeaiPipelineEnabled_defaults_to_false()
    {
        var previous = Environment.GetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar, null);
            MeaiPipelineServiceCollectionExtensions.IsMeaiPipelineEnabled(configuration: null)
                .Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar, previous);
        }
    }

    [Fact]
    public void IsMeaiPipelineEnabled_reads_env_var()
    {
        var previous = Environment.GetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar, "1");
            MeaiPipelineServiceCollectionExtensions.IsMeaiPipelineEnabled(configuration: null)
                .Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar, previous);
        }
    }

    [Fact]
    public void Feature_flag_config_key_enables_pipeline()
    {
        IConfiguration config = new ConfigurationBuilder()
            .Add(new SimpleKvSource(new Dictionary<string, string?>
            {
                [MeaiPipelineOptions.FeatureFlagKey] = "true",
            }))
            .Build();

        MeaiPipelineServiceCollectionExtensions.IsMeaiPipelineEnabled(config)
            .Should().BeTrue();
    }

    private sealed class SimpleKvSource(Dictionary<string, string?> values) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new SimpleKvProvider(values);
    }

    private sealed class SimpleKvProvider(Dictionary<string, string?> values) : ConfigurationProvider
    {
        public override void Load() => Data = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Explicit_false_overrides_env()
    {
        var previous = Environment.GetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar, "1");
            MeaiPipelineServiceCollectionExtensions.IsMeaiPipelineEnabled(configuration: null, explicitEnable: false)
                .Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar, previous);
        }
    }

    [Fact]
    public async Task Keyed_clients_resolve_and_respond_via_fakes()
    {
        var services = new ServiceCollection();
        services.AddNexoMeaiPipeline(
            ollamaInnerFactory: _ => new FakeChatClient("ollama-ok"),
            onnxInnerFactory: _ => new FakeChatClient("onnx-ok"));

        await using ServiceProvider provider = services.BuildServiceProvider();

        IChatClient ollama = provider.GetRequiredKeyedService<IChatClient>(MeaiTargetKeys.LocalOllama);
        IChatClient onnx = provider.GetRequiredKeyedService<IChatClient>(MeaiTargetKeys.LocalOnnx);

        ChatResponse ollamaResponse = await ollama.GetResponseAsync("hello");
        ChatResponse onnxResponse = await onnx.GetResponseAsync("hello");

        ollamaResponse.Text.Should().Be("ollama-ok");
        onnxResponse.Text.Should().Be("onnx-ok");
    }

    [Fact]
    public async Task Streaming_fake_client_yields_response()
    {
        var services = new ServiceCollection();
        services.AddNexoMeaiPipeline(
            ollamaInnerFactory: _ => new FakeChatClient("stream-ok"),
            onnxInnerFactory: _ => new FakeChatClient("stream-ok"));

        await using ServiceProvider provider = services.BuildServiceProvider();
        IChatClient client = provider.GetRequiredKeyedService<IChatClient>(MeaiTargetKeys.LocalOllama);

        var chunks = new List<string>();
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync("hi"))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                chunks.Add(update.Text);
            }
        }

        chunks.Should().ContainSingle().Which.Should().Be("stream-ok");
    }

    [Fact]
    public void Raw_provider_clients_are_not_resolvable_from_DI()
    {
        var services = new ServiceCollection();
        services.AddNexoMeaiPipeline(
            ollamaInnerFactory: _ => new FakeChatClient(),
            onnxInnerFactory: _ => new FakeChatClient());

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<OllamaHttpChatClient>().Should().BeNull();
        provider.GetKeyedService<OllamaHttpChatClient>(MeaiTargetKeys.LocalOllama).Should().BeNull();
        provider.GetService<LlamaSharpChatClient>().Should().BeNull();
        provider.GetKeyedService<LlamaSharpChatClient>(MeaiTargetKeys.LocalOnnx).Should().BeNull();
    }
}
