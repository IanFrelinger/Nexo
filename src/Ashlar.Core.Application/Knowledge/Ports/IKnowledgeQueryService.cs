using Ashlar.Core.Application.Knowledge.Models;

namespace Ashlar.Core.Application.Knowledge.Ports;

/// <summary>
/// Query façade over adaptation logs, pattern stores, and user knowledge logs.
/// </summary>
public interface IKnowledgeQueryService
{
    /// <summary>Executes a filtered knowledge query across configured sources.</summary>
    /// <param name="request">Time range, filters, pagination, and source selection.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Paged query result with provenance metadata per entry.</returns>
    Task<KnowledgeQueryResult> QueryAsync(KnowledgeQueryRequest request, CancellationToken cancellationToken = default);
}
