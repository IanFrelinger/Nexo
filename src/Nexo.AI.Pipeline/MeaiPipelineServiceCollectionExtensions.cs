using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.AI.Pipeline.Clients;

namespace Nexo.AI.Pipeline;

/// <summary>
/// DI registration for the MEAI chat pipeline (feature-flagged).
/// </summary>
public static class MeaiPipelineServiceCollectionExtensions
{
    /// <summary>
    /// Returns true when the MEAI pipeline should be registered.
    /// Resolution order: explicit <paramref name="explicitEnable"/> →
    /// <c>Nexo:UseMeaiPipeline</c> config → <c>NEXO_USE_MEAI_PIPELINE</c> env → false.
    /// </summary>
    public static bool IsMeaiPipelineEnabled(IConfiguration? configuration, bool? explicitEnable = null)
    {
        if (explicitEnable.HasValue)
        {
            return explicitEnable.Value;
        }

        if (configuration is not null)
        {
            var flagged = configuration[MeaiPipelineOptions.FeatureFlagKey];
            if (!string.IsNullOrWhiteSpace(flagged)
                && bool.TryParse(flagged, out var parsed))
            {
                return parsed;
            }

            if (string.Equals(flagged, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var env = Environment.GetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar);
        return string.Equals(env, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers keyed <see cref="IChatClient"/> pipelines for <c>local:ollama</c> and <c>local:onnx</c>.
    /// Raw provider clients (<c>OllamaApiClient</c>, LLamaSharp sessions) are never registered in DI.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Optional configuration for options binding.</param>
    /// <param name="configure">Optional options mutation.</param>
    /// <param name="ollamaInnerFactory">
    /// Optional replacement for the Ollama inner client (tests). When null, OllamaSharp is used.
    /// </param>
    /// <param name="onnxInnerFactory">
    /// Optional replacement for the local offline inner client (tests). When null, LLamaSharp is used.
    /// </param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNexoMeaiPipeline(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<MeaiPipelineOptions>? configure = null,
        Func<IServiceProvider, IChatClient>? ollamaInnerFactory = null,
        Func<IServiceProvider, IChatClient>? onnxInnerFactory = null)
    {
        var options = new MeaiPipelineOptions();
        if (configuration is not null)
        {
            BindSection(configuration.GetSection(MeaiPipelineOptions.SectionName), options);
        }

        configure?.Invoke(options);
        services.AddSingleton(Options.Create(options));

        // Keyed pipelines via MEAI ChatClientBuilder — inner factories stay private.
        Func<IServiceProvider, IChatClient> defaultOllama = sp =>
            new OllamaHttpChatClient(sp.GetRequiredService<IOptions<MeaiPipelineOptions>>());
        Func<IServiceProvider, IChatClient> defaultOnnx = sp =>
            new LlamaSharpChatClient(sp.GetRequiredService<IOptions<MeaiPipelineOptions>>());

        services.AddKeyedChatClient(
            MeaiTargetKeys.LocalOllama,
            sp => (ollamaInnerFactory ?? defaultOllama)(sp));

        services.AddKeyedChatClient(
            MeaiTargetKeys.LocalOnnx,
            sp => (onnxInnerFactory ?? defaultOnnx)(sp));

        return services;
    }

    private static void BindSection(IConfiguration section, MeaiPipelineOptions options)
    {
        // Lightweight bind without Configuration.Binder package dependency.
        var ollamaBase = section["OllamaBaseUrl"];
        if (!string.IsNullOrWhiteSpace(ollamaBase))
        {
            options.OllamaBaseUrl = ollamaBase;
        }

        var ollamaModel = section["OllamaModel"];
        if (!string.IsNullOrWhiteSpace(ollamaModel))
        {
            options.OllamaModel = ollamaModel;
        }

        var localPath = section["LocalModelPath"];
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            options.LocalModelPath = localPath;
        }

        if (int.TryParse(section["LocalContextSize"], out var ctx) && ctx > 0)
        {
            options.LocalContextSize = ctx;
        }

        if (int.TryParse(section["LocalMaxTokens"], out var max) && max > 0)
        {
            options.LocalMaxTokens = max;
        }
    }
}
