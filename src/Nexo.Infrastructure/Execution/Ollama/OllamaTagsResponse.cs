using System.Text.Json.Serialization;

namespace Nexo.Infrastructure.Execution.Ollama;

public sealed record OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public IReadOnlyList<OllamaTagsModel> Models { get; init; } = [];
}

public sealed record OllamaTagsModel
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("modified_at")]
    public DateTimeOffset? ModifiedAt { get; init; }
}
