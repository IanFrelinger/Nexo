namespace Ashlar.Tests.Infrastructure.Helpers;

/// <summary>
/// An <see cref="IProgress{T}"/> that invokes its handler synchronously, on the thread that
/// called <see cref="Report"/>.
///
/// <para><b>Why this exists.</b> <see cref="System.Progress{T}"/> does NOT invoke its callback
/// synchronously: it posts to the <c>SynchronizationContext</c> captured at construction, or to
/// the thread pool when there is none — which is the case under xUnit. So a test that does</para>
///
/// <code>
/// var reports = new List&lt;T&gt;();
/// await sut.DoWorkAsync(new Progress&lt;T&gt;(reports.Add));
/// reports.Should().ContainSingle();          // RACE
/// </code>
///
/// <para>is asserting against a callback that may not have run yet. The await completes when the
/// work completes, not when the posted callback drains. On an idle machine the pool usually gets
/// there first and the test passes; under a loaded pool — a full-solution run, a busy CI runner —
/// it does not, and the failure reads as "the collection is empty", pointing at the product
/// rather than at the harness.</para>
///
/// <para>Two tests were observed failing exactly that way and passing 3/3 in isolation:
/// <c>NullModelServingBackend_supports_full_lifecycle</c> and
/// <c>PullModelAsync_reports_progress_and_records_metrics</c>. Reporting synchronously removes
/// the race by construction rather than making it rarer.</para>
///
/// <para>Use this in tests instead of <see cref="System.Progress{T}"/>. Production code should
/// keep using <see cref="System.Progress{T}"/>, whose context-posting behaviour is the point.</para>
/// </summary>
/// <typeparam name="T">Progress value type.</typeparam>
public sealed class SyncProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    /// <summary>Creates a synchronous progress reporter.</summary>
    /// <param name="handler">Invoked inline on each report. Must not be null.</param>
    public SyncProgress(Action<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>Creates a synchronous progress reporter that discards every report.</summary>
    public SyncProgress()
    {
        _handler = static _ => { };
    }

    /// <summary>Invokes the handler inline, on the reporting thread.</summary>
    public void Report(T value) => _handler(value);
}
