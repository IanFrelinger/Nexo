using Microsoft.Extensions.DependencyInjection;
using Nexo.Orchestration.Assets.Ports;
using Nexo.Adapters.Assets.Audio;
using Nexo.Adapters.Assets.Images;
using Nexo.Adapters.Assets.Models3D;

namespace Nexo.Adapters.Assets;

/// <summary>
/// Extension methods for registering asset generation adapters.
/// 
/// Provides dependency injection registration for asset generators:
/// - Image generators (DALL-E, Local, Echo)
/// - Audio generators (Suno, Bark, ElevenLabs, Local, Echo)
/// - 3D model generators (Meshy, Tripo, Local, Echo)
/// 
/// Call AddAssetGenerators() with configuration options to register generators.
/// Defaults to Echo (placeholder) generators if not configured.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds asset generation services with real API implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Configuration options for which generators to use.</param>
    public static IServiceCollection AddAssetGenerators(
        this IServiceCollection services,
        Action<AssetGeneratorOptions>? configure = null)
    {
        var options = new AssetGeneratorOptions();
        configure?.Invoke(options);

        // Image generators
        switch (options.ImageGenerator)
        {
            case ImageGeneratorType.Dalle:
                services.AddHttpClient<IImageGenerator, DalleImageGenerator>();
                break;
            case ImageGeneratorType.Local:
                services.AddHttpClient<IImageGenerator, LocalImageGenerator>();
                break;
            default:
                services.AddSingleton<IImageGenerator, EchoImageGenerator>();
                break;
        }

        // Audio generators
        switch (options.AudioGenerator)
        {
            case AudioGeneratorType.Suno:
                services.AddHttpClient<IAudioGenerator, SunoAudioGenerator>();
                break;
            case AudioGeneratorType.Bark:
                services.AddHttpClient<IAudioGenerator, BarkAudioGenerator>();
                break;
            case AudioGeneratorType.ElevenLabs:
                services.AddHttpClient<IAudioGenerator, ElevenLabsAudioGenerator>();
                break;
            case AudioGeneratorType.Local:
                services.AddHttpClient<IAudioGenerator, LocalAudioGenerator>();
                break;
            default:
                services.AddSingleton<IAudioGenerator, EchoAudioGenerator>();
                break;
        }

        // 3D model generators
        switch (options.Model3DGenerator)
        {
            case Model3DGeneratorType.Meshy:
                services.AddHttpClient<IModel3DGenerator, MeshyModelGenerator>();
                break;
            case Model3DGeneratorType.Tripo:
                services.AddHttpClient<IModel3DGenerator, TripoModelGenerator>();
                break;
            case Model3DGeneratorType.Local:
                services.AddHttpClient<IModel3DGenerator, LocalModel3DGenerator>();
                break;
            default:
                services.AddSingleton<IModel3DGenerator, EchoModel3DGenerator>();
                break;
        }

        return services;
    }
}

/// <summary>
/// Configuration options for asset generators.
/// </summary>
public class AssetGeneratorOptions
{
    /// <summary>
    /// Image generator to use.
    /// </summary>
    public ImageGeneratorType ImageGenerator { get; set; } = ImageGeneratorType.Echo;

    /// <summary>
    /// Audio generator to use.
    /// </summary>
    public AudioGeneratorType AudioGenerator { get; set; } = AudioGeneratorType.Echo;

    /// <summary>
    /// 3D model generator to use.
    /// </summary>
    public Model3DGeneratorType Model3DGenerator { get; set; } = Model3DGeneratorType.Echo;
}

/// <summary>
/// Image generator types.
/// </summary>
public enum ImageGeneratorType
{
    /// <summary>
    /// Echo placeholder (no API calls).
    /// </summary>
    Echo,

    /// <summary>
    /// DALL-E 3 for image generation (requires OpenAI API key).
    /// </summary>
    Dalle,

    /// <summary>
    /// Local/self-hosted image generation (Stable Diffusion, Ollama, LocalAI, etc.).
    /// </summary>
    Local
}

/// <summary>
/// Audio generator types.
/// </summary>
public enum AudioGeneratorType
{
    /// <summary>
    /// Echo placeholder (no API calls).
    /// </summary>
    Echo,

    /// <summary>
    /// Suno AI for music generation.
    /// </summary>
    Suno,

    /// <summary>
    /// Bark AI for music, sound effects, and speech.
    /// </summary>
    Bark,

    /// <summary>
    /// ElevenLabs for speech synthesis.
    /// </summary>
    ElevenLabs,

    /// <summary>
    /// Local/self-hosted audio generation (Bark local, Piper TTS, Coqui TTS, etc.).
    /// </summary>
    Local
}

/// <summary>
/// 3D model generator types.
/// </summary>
public enum Model3DGeneratorType
{
    /// <summary>
    /// Echo placeholder (no API calls).
    /// </summary>
    Echo,

    /// <summary>
    /// Meshy AI for 3D model generation.
    /// </summary>
    Meshy,

    /// <summary>
    /// Tripo AI for 3D model generation.
    /// </summary>
    Tripo,

    /// <summary>
    /// Local/self-hosted 3D model generation (custom endpoints).
    /// </summary>
    Local
}

