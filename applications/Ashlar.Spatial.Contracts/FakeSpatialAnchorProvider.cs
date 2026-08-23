using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;

namespace Ashlar.Spatial.Contracts;

/// <summary>
/// Deterministic scripted pose provider for headless tests.
/// </summary>
public sealed class FakeSpatialAnchorProvider : ISpatialAnchorProvider, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, ScriptedAtomSequence> _sequences;
    private readonly Dictionary<string, Subject<PoseSample>> _subjects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _indices = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private bool _disposed;

    /// <summary>Creates a fake provider from an in-memory scripted sequence list.</summary>
    /// <param name="sequences">Per-atom scripted pose timelines.</param>
    public FakeSpatialAnchorProvider(IEnumerable<ScriptedAtomSequence> sequences)
    {
        _sequences = sequences.ToDictionary(s => s.AtomId, StringComparer.Ordinal);
    }

    /// <summary>Deserializes scripted sequences from JSON (camelCase property names).</summary>
    /// <param name="json">JSON array of <see cref="ScriptedAtomSequence"/> objects.</param>
    public static FakeSpatialAnchorProvider FromJson(string json) =>
        new(JsonSerializer.Deserialize<ScriptedAtomSequence[]>(json, JsonOptions) ?? Array.Empty<ScriptedAtomSequence>());

    /// <inheritdoc />
    public Task<PoseSample?> GetCurrentPose(string atomId)
    {
        lock (_gate)
        {
            if (!_sequences.TryGetValue(atomId, out var sequence) || sequence.Samples.Count == 0)
                return Task.FromResult<PoseSample?>(null);

            var index = _indices.TryGetValue(atomId, out var current) ? current : 0;
            index = Math.Max(0, Math.Min(index, sequence.Samples.Count - 1));
            return Task.FromResult<PoseSample?>(sequence.Samples[index]);
        }
    }

    /// <inheritdoc />
    public IObservable<PoseSample> ObservePose(string atomId)
    {
        lock (_gate)
        {
            if (!_subjects.TryGetValue(atomId, out var subject))
            {
                subject = new Subject<PoseSample>();
                _subjects[atomId] = subject;
            }

            return subject.AsObservable();
        }
    }

    /// <summary>
    /// Publishes the next scripted sample for an atom to all subscribers.
    /// </summary>
    public void PublishNext(string atomId)
    {
        lock (_gate)
        {
            ObjectDisposedGuard();
            if (!_sequences.TryGetValue(atomId, out var sequence) || sequence.Samples.Count == 0)
                return;

            var index = _indices.TryGetValue(atomId, out var current) ? current : 0;
            if (index >= sequence.Samples.Count)
                return;

            var sample = sequence.Samples[index];
            _indices[atomId] = index + 1;

            if (_subjects.TryGetValue(atomId, out var subject) && !subject.IsDisposed)
                subject.OnNext(sample);
        }
    }

    /// <summary>
    /// Publishes an explicit sample (for rejection tests).
    /// </summary>
    public void Publish(string atomId, PoseSample sample)
    {
        lock (_gate)
        {
            ObjectDisposedGuard();
            if (_subjects.TryGetValue(atomId, out var subject) && !subject.IsDisposed)
                subject.OnNext(sample);
        }
    }

    /// <summary>Releases all pose observables and completes active subscriptions.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var subject in _subjects.Values)
            {
                subject.OnCompleted();
                subject.Dispose();
            }
        }
    }

    private void ObjectDisposedGuard()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FakeSpatialAnchorProvider));
    }
}
