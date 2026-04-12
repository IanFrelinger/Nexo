using Nexo.Core.Application.Knowledge.Models;

namespace Nexo.Core.Application.Knowledge.Ports;

public interface IKnowledgeQueryService
{
    Task<KnowledgeQueryResult> QueryAsync(KnowledgeQueryRequest request, CancellationToken cancellationToken = default);
}
