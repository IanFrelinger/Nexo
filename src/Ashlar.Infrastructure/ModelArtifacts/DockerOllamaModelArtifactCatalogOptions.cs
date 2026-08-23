namespace Ashlar.Infrastructure.ModelArtifacts;

/// <summary>Options for discovering Ollama models inside Docker-hosted Ollama containers.</summary>
public sealed class DockerOllamaModelArtifactCatalogOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ashlar:ModelArtifactCatalog:DockerOllama";

    /// <summary>When false, <see cref="DockerOllamaModelArtifactCatalogSource"/> returns no entries.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Match <see cref="Docker.DotNet.Models.ContainerListResponse.Image"/> prefix (e.g. <c>ollama/ollama</c>).</summary>
    public string OllamaImagePrefix { get; set; } = "ollama/ollama";

    /// <summary>Container port that exposes the Ollama HTTP API.</summary>
    public int OllamaContainerPort { get; set; } = 11434;
}
