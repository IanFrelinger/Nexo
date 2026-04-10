using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Models;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class ProviderBackedModelDirectiveTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestProviderAndModelDirectivesAreForwardedAsync().ConfigureAwait(false);
            return new TestResult
            {
                Name = nameof(ProviderBackedModelDirectiveTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "ProviderBackedModel directive tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ProviderBackedModelDirectiveTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(ProviderBackedModelDirectiveTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestProviderAndModelDirectivesAreForwardedAsync()
    {
        var providerFactory = new CapturingProviderFactory();
        var model = new ProviderBackedModel(providerFactory, NullLogger<ProviderBackedModel>.Instance);
        var input = new ModelInput(new List<(string role, string content)>
        {
            ("system", "nexo.model.provider=ollama\nnexo.model.name=qwen2.5:7b\nSystem behavior"),
            ("user", "Run task")
        });

        var output = await model.CompleteAsync(input, CancellationToken.None).ConfigureAwait(false);

        AssertEqual("ok", output.Text);
        AssertEqual("ollama", providerFactory.LastProvider);
        AssertEqual("qwen2.5:7b", providerFactory.LastModel);
        AssertTrue((providerFactory.LastSystemPrompt ?? string.Empty).Contains("System behavior", StringComparison.Ordinal));
    }

    private sealed class CapturingProviderFactory : IProviderFactory
    {
        public string? LastProvider { get; private set; }
        public string? LastSystemPrompt { get; private set; }
        public string? LastUserPrompt { get; private set; }
        public string? LastModel { get; private set; }

        public bool IsProviderAvailable(string provider) => true;

        public Task<string> ExecuteLLMAsync(
            string provider,
            string systemPrompt,
            string userPrompt,
            object config,
            CancellationToken cancellationToken = default)
        {
            LastProvider = provider;
            LastSystemPrompt = systemPrompt;
            LastUserPrompt = userPrompt;
            LastModel = ReadModel(config);
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

        private static string? ReadModel(object config)
        {
            var prop = config.GetType().GetProperty("model");
            return prop?.GetValue(config) as string;
        }
    }
}
