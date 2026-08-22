namespace Ashlar.Agents.TestKit;

/// <summary>
/// A scripted sequence of outcomes consumed one per call. This is the primitive
/// that makes FAILURE A FIRST-CLASS INPUT: a fake is handed the exact run of
/// outcomes a real edge would produce — <c>[fail, fail, succeed]</c> — so a test
/// can assert what the orchestrator does BETWEEN them.
/// </summary>
/// <typeparam name="T">Outcome type.</typeparam>
public sealed class Scripted<T>
{
    private readonly IReadOnlyList<T> _outcomes;
    private int _index;

    /// <summary>Creates a sequence from an explicit script.</summary>
    /// <param name="outcomes">Outcomes in the order they should be returned.</param>
    /// <param name="repeatLast">
    /// When true (default) the final outcome repeats once the script is exhausted;
    /// when false, an extra call throws so over-calling is caught rather than masked.
    /// </param>
    public Scripted(IEnumerable<T> outcomes, bool repeatLast = true)
    {
        _outcomes = (outcomes ?? throw new ArgumentNullException(nameof(outcomes))).ToArray();
        if (_outcomes.Count == 0)
            throw new ArgumentException("A scripted sequence needs at least one outcome.", nameof(outcomes));
        RepeatLast = repeatLast;
    }

    /// <summary>Whether the last outcome repeats after exhaustion.</summary>
    public bool RepeatLast { get; }

    /// <summary>How many times an outcome has been taken.</summary>
    public int Calls { get; private set; }

    /// <summary>Outcomes remaining before exhaustion.</summary>
    public int Remaining => Math.Max(0, _outcomes.Count - _index);

    /// <summary>Takes the next outcome.</summary>
    public T Next()
    {
        Calls++;
        if (_index < _outcomes.Count)
            return _outcomes[_index++];

        if (RepeatLast)
            return _outcomes[^1];

        throw new InvalidOperationException(
            $"Scripted<{typeof(T).Name}> exhausted after {_outcomes.Count} outcome(s) but was called {Calls} time(s).");
    }

    /// <summary>Builds a single-outcome sequence.</summary>
    public static Scripted<T> Always(T outcome) => new(new[] { outcome });

    /// <summary>Builds a sequence from params.</summary>
    public static Scripted<T> Of(params T[] outcomes) => new(outcomes);
}
