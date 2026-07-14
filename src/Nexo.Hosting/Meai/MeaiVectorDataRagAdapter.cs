using Nexo.AI.Pipeline.Rag;
using Nexo.BackgroundAgents.RAG;

namespace Nexo.Hosting.Meai;

/// <summary>
/// Adapts <see cref="VectorDataRagService"/> to the legacy <see cref="IRAGService"/> surface
/// so tools/CLI keep working after Phase 6 cutover.
/// </summary>
public sealed class MeaiVectorDataRagAdapter : IRAGService
{
    private readonly VectorDataRagService _rag;

    /// <summary>Creates the adapter.</summary>
    public MeaiVectorDataRagAdapter(VectorDataRagService rag) =>
        _rag = rag ?? throw new ArgumentNullException(nameof(rag));

    /// <inheritdoc />
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int maxResults,
        double minScore,
        string? maxSensitivityLevelName,
        CancellationToken cancellationToken = default)
    {
        var tier = string.IsNullOrWhiteSpace(maxSensitivityLevelName) ? "TopSecret" : maxSensitivityLevelName.Trim();
        var hits = await _rag.SearchAsync(
                query ?? string.Empty,
                callerMaxTrustTier: tier,
                top: maxResults,
                minScore: minScore,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return hits
            .Select(h => new VectorSearchResult(
                h.Record.Key,
                h.Record.Text,
                h.Score ?? 0d,
                h.Record.TrustTier))
            .ToList();
    }

    /// <inheritdoc />
    public Task IndexAsync(
        string id,
        string text,
        string? sensitivityLevelName,
        CancellationToken cancellationToken = default) =>
        _rag.IndexAsync(
            id,
            text ?? string.Empty,
            sourceUri: null,
            trustTier: string.IsNullOrWhiteSpace(sensitivityLevelName) ? "Public" : sensitivityLevelName.Trim(),
            cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string id, CancellationToken cancellationToken = default) =>
        _rag.RemoveAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _rag.ClearAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _rag.GetDocumentCountAsync(cancellationToken).ConfigureAwait(false);
        return count < 0 ? 0 : count;
    }
}
