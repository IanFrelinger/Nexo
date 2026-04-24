using System.Text.Json;
using Microsoft.Extensions.Http;
using Nexo.Core.Application.ModelArtifacts;
using Nexo.Core.Application.ModelArtifacts.Ports;
using Nexo.Infrastructure.Execution.Ollama;

namespace Nexo.Infrastructure.ModelArtifacts;

/// <summary>
/// Lists Ollama models installed on the daemon via <c>GET /api/tags</c> (same as <c>ollama list</c>).
/// </summary>
public sealed class OllamaTagsModelArtifactCatalogSource : IModelArtifactCatalogSource
{
    public const string HttpClientName = "Nexo.ModelArtifactCatalog.OllamaTags";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaTagsModelArtifactCatalogSource(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public string SourceId => "ollama-tags";

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<ModelArtifactRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        OllamaTagsResponse? body;
        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            body = await JsonSerializer.DeserializeAsync<OllamaTagsResponse>(stream, JsonOptions, cancellationToken)
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

            list.Add(new ModelArtifactRecord(
                Id: m.Name.Trim(),
                SourceId: SourceId,
                Kind: ModelArtifactKind.OllamaModel,
                SizeHintBytes: m.Size,
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["modified_at"] = m.ModifiedAt?.ToString("O") ?? string.Empty
                }));
        }

        return list;
    }
}
