using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.ModelArtifacts;
using Nexo.Core.Application.ModelArtifacts.Ports;

namespace Nexo.Infrastructure.ModelArtifacts;

/// <summary>
/// Lists models published on <see href="https://ollama.com">ollama.com</see> for install via
/// <c>ollama pull</c>, using the public <c>GET /api/tags</c> document (same shape as the local daemon).
/// </summary>
public sealed class OllamaRemoteLibraryModelArtifactCatalogSource : IRemoteInstallableModelArtifactSource
{
    public const string HttpClientName = "Nexo.ModelArtifactCatalog.OllamaRemoteLibrary";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<OllamaRemoteLibraryCatalogOptions> _options;

    public OllamaRemoteLibraryModelArtifactCatalogSource(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<OllamaRemoteLibraryCatalogOptions> options)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string SourceId => "ollama-remote-library";

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_options.CurrentValue.Enabled);

    public async Task<IReadOnlyList<ModelArtifactRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled)
        {
            return [];
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        RemoteWireResponse? body;
        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            body = await JsonSerializer.DeserializeAsync<RemoteWireResponse>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        if (body?.Models is null || body.Models.Count == 0)
        {
            return [];
        }

        var list = new List<ModelArtifactRecord>(body.Models.Count);
        foreach (var m in body.Models)
        {
            if (string.IsNullOrWhiteSpace(m.Name))
            {
                continue;
            }

            var id = m.Name.Trim();
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["modified_at"] = m.ModifiedAt?.ToString("O") ?? string.Empty
            };
            if (!string.IsNullOrWhiteSpace(m.Digest))
            {
                meta["digest"] = m.Digest.Trim();
            }

            list.Add(new ModelArtifactRecord(
                id,
                SourceId,
                ModelArtifactKind.OllamaRemoteLibraryModel,
                m.Size,
                meta));
        }

        return list;
    }

    private sealed class RemoteWireResponse
    {
        [JsonPropertyName("models")]
        public List<RemoteWireModel> Models { get; init; } = [];
    }

    private sealed class RemoteWireModel
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("modified_at")]
        public DateTimeOffset? ModifiedAt { get; init; }

        [JsonPropertyName("digest")]
        public string? Digest { get; init; }
    }
}
