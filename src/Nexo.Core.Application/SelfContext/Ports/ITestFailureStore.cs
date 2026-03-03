using Nexo.Core.Application.SelfContext.Models;

namespace Nexo.Core.Application.SelfContext.Ports;

/// <summary>
/// Stores test failures for adaptation trigger (Phase F).
/// When tests fail, record them; improve flow can query and trigger adaptation.
/// </summary>
public interface ITestFailureStore
{
    Task RecordAsync(TestFailureRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestFailureRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default);
}
