using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Models;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class ProviderBackedModelDirectiveTests
{
    [Fact]
    public async Task CompleteAsync_ParsesModelDirective_AndPassesModelConfig()
    {
        var providerFactory = new CapturingProviderFactory();
        var model = new ProviderBackedModel(providerFactory, NullLogger<ProviderBackedModel>.Instance);

        var output = await model.CompleteAsync(
            new ModelInput(new List<(string role, string content)>
            {
                ("system", "nexo.model.provider=ollama\nnexo.model.name=llama3.2:3b\nretain this"),
                ("user", "hello world")
            }),
            CancellationToken.None);

        output.Text.Should().Be("ok");
        providerFactory.CapturedProvider.Should().Be("ollama");
        providerFactory.CapturedSystemPrompt.Should().Contain("retain this");
        providerFactory.CapturedModelName.Should().Be("llama3.2:3b");
    }

    private sealed class CapturingProviderFactory : IProviderFactory
    {
        public string? CapturedProvider { get; private set; }
        public string? CapturedSystemPrompt { get; private set; }
        public string? CapturedModelName { get; private set; }

        public bool IsProviderAvailable(string provider) => true;

        public Task<string> ExecuteLLMAsync(
            string provider,
            string systemPrompt,
            string userPrompt,
            object config,
            CancellationToken cancellationToken = default)
        {
            CapturedProvider = provider;
            CapturedSystemPrompt = systemPrompt;
            var modelProp = config.GetType().GetProperty("model");
            CapturedModelName = modelProp?.GetValue(config) as string;
            return Task.FromResult("ok");
        }

        public Task<string> ExecuteVisionAsync(
            string provider,
            string systemPrompt,
            string userPrompt,
            byte[] imageBytes,
            object config,
            CancellationToken cancellationToken = default)
            => Task.FromResult("ok");

        public Task<string> ExecuteVisionMultiFrameAsync(
            string provider,
            string systemPrompt,
            string userPrompt,
            IReadOnlyList<byte[]> frameBytes,
            object config,
            CancellationToken cancellationToken = default)
            => Task.FromResult("ok");

        public Task<string> ExecuteVideoAsync(
            string systemPrompt,
            string userPrompt,
            IReadOnlyList<byte[]> frameBytes,
            object config,
            CancellationToken cancellationToken = default)
            => Task.FromResult("ok");

        public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
