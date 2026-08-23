using Ashlar.Core.Application.NodeCapabilityRuntime.Models;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime;

/// <summary>Default catalog of models seeded per platform for node capability runtime routing.</summary>
internal static class DefaultModelSuite
{
    /// <summary>Returns the built-in model descriptors appropriate for <paramref name="platform"/>.</summary>
    public static List<ModelDescriptor> CreateForPlatform(PlatformType platform)
    {
        var desktopOnly = new HashSet<PlatformType> { PlatformType.Windows, PlatformType.macOS, PlatformType.Linux };
        var allPlatforms = new HashSet<PlatformType>
        {
            PlatformType.Windows,
            PlatformType.macOS,
            PlatformType.Linux,
            PlatformType.iOS,
            PlatformType.Android
        };

        return new List<ModelDescriptor>
        {
            new()
            {
                Id = "phi3-mini",
                DisplayName = "Phi-3 Mini",
                Provider = "ollama",
                ProviderModelId = "phi3:mini",
                State = ModelState.Warm,
                QualityScore = 0.62f,
                SpeedScore = 0.90f,
                WeightSizeBytes = 2L * 1024 * 1024 * 1024,
                MinRAMRequiredBytes = 3L * 1024 * 1024 * 1024,
                Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
                SupportedPlatforms = allPlatforms
            },
            new()
            {
                Id = "llama3-8b",
                DisplayName = "Llama 3 8B Instruct",
                Provider = "ollama",
                ProviderModelId = "llama3:8b-instruct-q4_K_M",
                State = ModelState.Cold,
                QualityScore = 0.80f,
                SpeedScore = 0.68f,
                WeightSizeBytes = 5L * 1024 * 1024 * 1024,
                MinVRAMRequiredBytes = 6L * 1024 * 1024 * 1024,
                MinRAMRequiredBytes = 10L * 1024 * 1024 * 1024,
                Capabilities = new HashSet<TaskCapability>
                {
                    TaskCapability.TextGeneration,
                    TaskCapability.Reasoning
                },
                SupportedPlatforms = desktopOnly
            },
            new()
            {
                Id = "qwen-coder-7b",
                DisplayName = "Qwen2.5 Coder 7B",
                Provider = "ollama",
                ProviderModelId = "qwen2.5-coder:7b",
                State = ModelState.Warm,
                QualityScore = 0.86f,
                SpeedScore = 0.64f,
                WeightSizeBytes = 5L * 1024 * 1024 * 1024,
                MinVRAMRequiredBytes = 6L * 1024 * 1024 * 1024,
                MinRAMRequiredBytes = 10L * 1024 * 1024 * 1024,
                Capabilities = new HashSet<TaskCapability>
                {
                    TaskCapability.CodeGeneration,
                    TaskCapability.TextGeneration
                },
                SupportedPlatforms = desktopOnly
            },
            new()
            {
                Id = "nomic-embed-text",
                DisplayName = "nomic-embed-text",
                Provider = "ollama",
                ProviderModelId = "nomic-embed-text",
                State = ModelState.Hot,
                QualityScore = 0.75f,
                SpeedScore = 0.95f,
                WeightSizeBytes = 512L * 1024 * 1024,
                MinRAMRequiredBytes = 1024L * 1024 * 1024,
                Capabilities = new HashSet<TaskCapability> { TaskCapability.Embeddings },
                SupportedPlatforms = allPlatforms
            }
        };
    }
}
