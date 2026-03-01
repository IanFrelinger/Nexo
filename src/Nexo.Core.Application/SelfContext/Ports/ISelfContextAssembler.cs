using Nexo.Core.Application.SelfContext.Models;

namespace Nexo.Core.Application.SelfContext.Ports;

/// <summary>
/// Assembles self-context from adaptation log, execution tracer, and pattern store.
/// </summary>
public interface ISelfContextAssembler
{
    Task<SelfContextModel> AssembleAsync(TimeSpan? lookback = null, CancellationToken cancellationToken = default);
}
