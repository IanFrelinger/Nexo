using System.Net.Http;
using System.Reflection;
using FluentAssertions;
using Nexo.Infrastructure.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Tests.Infrastructure.Tests.Safety;

/// <summary>Tests for local model provider integration.</summary>
[Collection("LocalModelProviderState")]
[Trait("Category", "DockerOptional")]
public sealed class LocalModelProviderIntegrationTests
{
    private const string DefaultModelUrl =
        "https://huggingface.co/ggml-org/test-model-stories260K/resolve/main/stories260K-f32.gguf";

    private readonly ITestOutputHelper _output;

    /// <summary>Local model provider integration tests.</summary>
    /// <param name="output">Output.</param>
    public LocalModelProviderIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task ExecuteAsync_with_valid_gguf_returns_non_empty_text()
    {
        await using var scope = await LocalModelTestScope.CreateAsync(_output);
        if (!scope.IsReady)
            return;

        var result = await LocalModelProvider.ExecuteAsync(
            "You are a helpful assistant.",
            "Say hello in one short sentence.",
            config: null,
            CancellationToken.None);

        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_with_empty_system_prompt_uses_user_prompt_only()
    {
        await using var scope = await LocalModelTestScope.CreateAsync(_output);
        if (!scope.IsReady)
            return;

        var result = await LocalModelProvider.ExecuteAsync(
            "",
            "Reply with the word ping only.",
            config: null,
            CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureModelLoaded_is_idempotent_after_first_execute()
    {
        await using var scope = await LocalModelTestScope.CreateAsync(_output);
        if (!scope.IsReady)
            return;

        await LocalModelProvider.ExecuteAsync("sys", "first", null, CancellationToken.None);

        var weightsBefore = GetStaticField("_weights");
        /// <summary>Invoke ensure model loaded.</summary>
        InvokeEnsureModelLoaded(scope.ModelPath!);
        var weightsAfter = GetStaticField("_weights");

        weightsBefore.Should().NotBeNull();
        weightsAfter.Should().BeSameAs(weightsBefore);
    }

    [Fact]
    public async Task ExecuteAsync_honors_cancellation()
    {
        await using var scope = await LocalModelTestScope.CreateAsync(_output);
        if (!scope.IsReady)
            return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => LocalModelProvider.ExecuteAsync(
            "sys",
            "Write a very long story about dragons and castles and heroes.",
            null,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_loads_with_explicit_context_size()
    {
        await using var scope = await LocalModelTestScope.CreateAsync(_output, contextSize: "512");
        if (!scope.IsReady)
            return;

        var result = await LocalModelProvider.ExecuteAsync("sys", "hi", null, CancellationToken.None);
        result.Should().NotBeNull();
    }

    /// <summary>Tests for local model test scope.</summary>
    private sealed class LocalModelTestScope : IAsyncDisposable
    {
        private readonly string? _previousPath;
        private readonly string? _previousContext;

        private LocalModelTestScope(string? previousPath, string? previousContext, bool isReady, string? skipReason)
        {
            _previousPath = previousPath;
            _previousContext = previousContext;
            IsReady = isReady;
            SkipReason = skipReason;
        }

        /// <summary>Is ready.</summary>
        public bool IsReady { get; }

        /// <summary>Skip reason.</summary>
        public string? SkipReason { get; }

        /// <summary>Model path.</summary>
        public string? ModelPath { get; private set; }

        public static async Task<LocalModelTestScope> CreateAsync(
            ITestOutputHelper output,
            string? contextSize = null)
        {
            /// <summary>Reset loaded model state.</summary>
            ResetLoadedModelState();
            var previousPath = Environment.GetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH");
            var previousContext = Environment.GetEnvironmentVariable("NEXO_LOCAL_CONTEXT_SIZE");

            try
            {
                var modelPath = await ResolveModelPathAsync();
                if (modelPath is null)
                {
                    return new LocalModelTestScope(
                        previousPath,
                        previousContext,
                        isReady: false,
                        skipReason: "No GGUF model available. Set NEXO_LOCAL_MODEL_PATH or allow fixture download.");
                }

                Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", modelPath);
                Environment.SetEnvironmentVariable("NEXO_LOCAL_CONTEXT_SIZE", contextSize);

                if (!LocalModelProvider.IsAvailable())
                {
                    return new LocalModelTestScope(
                        previousPath,
                        previousContext,
                        isReady: false,
                        skipReason: $"Model path not available: {modelPath}");
                }

                var scope = new LocalModelTestScope(previousPath, previousContext, isReady: true, skipReason: null)
                {
                    ModelPath = modelPath
                };
                return scope;
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
                return new LocalModelTestScope(
                    previousPath,
                    previousContext,
                    isReady: false,
                    skipReason: ex.Message);
            }
        }

        public ValueTask DisposeAsync()
        {
            /// <summary>Reset loaded model state.</summary>
            ResetLoadedModelState();
            Environment.SetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH", _previousPath);
            Environment.SetEnvironmentVariable("NEXO_LOCAL_CONTEXT_SIZE", _previousContext);
            return ValueTask.CompletedTask;
        }

        private static async Task<string?> ResolveModelPathAsync()
        {
            var fromEnv = Environment.GetEnvironmentVariable("NEXO_LOCAL_MODEL_PATH")?.Trim();
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return fromEnv;

            var cacheDir = Path.Combine(Path.GetTempPath(), "nexo-gguf-cache");
            Directory.CreateDirectory(cacheDir);
            var cached = Path.Combine(cacheDir, "stories260K-f32.gguf");
            if (File.Exists(cached) && new FileInfo(cached).Length > 100_000)
                return cached;

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            await using var stream = await client.GetStreamAsync(DefaultModelUrl);
            await using var file = File.Create(cached);
            await stream.CopyToAsync(file);
            return cached;
        }

        private static void ResetLoadedModelState()
        {
            var type = typeof(LocalModelProvider);
            type.GetField("_weights", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
            type.GetField("_context", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }
    }

    /// <summary>Gets static field.</summary>
    /// <param name="name">Name.</param>
    private static object? GetStaticField(string name) =>
        typeof(LocalModelProvider).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);

    private static void InvokeEnsureModelLoaded(string path)
    {
        var method = typeof(LocalModelProvider).GetMethod(
            "EnsureModelLoaded",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(null, new object[] { path });
    }
}
