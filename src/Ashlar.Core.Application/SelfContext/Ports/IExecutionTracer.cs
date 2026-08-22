using Ashlar.Core.Application.SelfContext.Models;

namespace Ashlar.Core.Application.SelfContext.Ports;

/// <summary>
/// Traces execution of operations (improve, adapt, observe).
/// </summary>
public interface IExecutionTracer
{
    Task TraceAsync(string operation, IReadOnlyDictionary<string, object>? context = null, string? path = null, string? outcome = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExecutionTraceEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default);
}
