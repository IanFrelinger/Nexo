using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;

namespace Nexo.Infrastructure.SelfImprovement;

/// <summary>
/// Bridges parsed test results (from TRX or other formats) into the
/// ITestFailureStore, enabling the self-improvement loop to pick up
/// failures from real test runs.
/// </summary>
public sealed class TestFailureIngestionBridge
{
    private readonly ITestFailureStore _store;

    public TestFailureIngestionBridge(ITestFailureStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Ingests failed test results into the test failure store.
    /// Only records failures (Passed=false). Returns count of failures ingested.
    /// </summary>
    public async Task<int> IngestAsync(
        IReadOnlyList<TestResult> results,
        CancellationToken ct = default)
    {
        var failures = results.Where(r => !r.Passed).ToList();
        foreach (var failure in failures)
        {
            var record = new TestFailureRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.UtcNow,
                TestName = failure.Name,
                FilePath = null,
                ErrorMessage = failure.ErrorMessage ?? failure.Message,
                StackTrace = failure.StackTrace,
            };
            await _store.RecordAsync(record, ct).ConfigureAwait(false);
        }
        return failures.Count;
    }
}
