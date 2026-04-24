using Microsoft.Extensions.Logging;
using Nexo.Core.Application.ModelArtifacts;
using Nexo.Core.Application.ModelArtifacts.Ports;

namespace Nexo.Infrastructure.ModelArtifacts;

/// <summary>
/// Merges all registered <see cref="IModelArtifactCatalogSource"/> implementations.
/// Sources that are unavailable or throw are skipped.
/// </summary>
public sealed class ModelArtifactCatalogService : IModelArtifactCatalogService
{
    private readonly IEnumerable<IModelArtifactCatalogSource> _sources;
    private readonly ILogger<ModelArtifactCatalogService> _logger;

    public ModelArtifactCatalogService(
        IEnumerable<IModelArtifactCatalogSource> sources,
        ILogger<ModelArtifactCatalogService> logger)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ModelArtifactRecord>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<ModelArtifactRecord>();
        foreach (var source in _sources)
        {
            try
            {
                if (!await source.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                var chunk = await source.ListAsync(cancellationToken).ConfigureAwait(false);
                if (chunk.Count > 0)
                {
                    list.AddRange(chunk);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Model artifact catalog source {SourceId} failed; continuing with other sources", source.SourceId);
            }
        }

        return list;
    }
}
