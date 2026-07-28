using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Agents.TestKit;

/// <summary>One recorded apply against the fake file set.</summary>
/// <param name="DestRoot">Destination root the caller asked for.</param>
/// <param name="Files">Relative path → content that was written.</param>
public sealed record RecordedApply(string DestRoot, IReadOnlyDictionary<string, string> Files);

/// <summary>
/// In-memory <see cref="IManagedFileSet"/> that records applies and rollbacks and
/// models snapshot semantics: rollback restores exactly the contents that existed
/// before the apply. Lets a test assert snapshot-then-rollback without touching disk.
/// </summary>
public sealed class FakeManagedFileSet : IManagedFileSet
{
    private readonly Dictionary<string, string> _state = new(StringComparer.OrdinalIgnoreCase);
    private readonly Scripted<bool>? _applySucceeds;

    /// <summary>Creates a file set whose applies always succeed.</summary>
    public FakeManagedFileSet() { }

    /// <summary>Creates a file set with a scripted run of apply outcomes.</summary>
    public FakeManagedFileSet(Scripted<bool> applySucceeds) => _applySucceeds = applySucceeds;

    /// <summary>Applies recorded in order.</summary>
    public List<RecordedApply> Applies { get; } = new();

    /// <summary>How many times a rollback was invoked.</summary>
    public int RollbackCalls { get; private set; }

    /// <summary>Current contents, keyed by "destRoot/relativePath".</summary>
    public IReadOnlyDictionary<string, string> Contents => _state;

    /// <summary>Seeds pre-existing content so a rollback has something to restore to.</summary>
    public FakeManagedFileSet Seed(string destRoot, string relativePath, string content)
    {
        _state[Key(destRoot, relativePath)] = content;
        return this;
    }

    /// <inheritdoc />
    public Task<ManagedFileApplyResult> ApplyAsync(
        string destRoot,
        IReadOnlyDictionary<string, string> files,
        CancellationToken cancellationToken = default)
    {
        if (_applySucceeds is not null && !_applySucceeds.Next())
        {
            return Task.FromResult(new ManagedFileApplyResult
            {
                Succeeded = false,
                Rollback = _ =>
                {
                    RollbackCalls++;
                    return Task.CompletedTask;
                }
            });
        }

        // Snapshot exactly what the apply is about to disturb.
        var snapshot = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var rel in files.Keys)
        {
            var key = Key(destRoot, rel);
            snapshot[key] = _state.TryGetValue(key, out var prior) ? prior : null;
        }

        foreach (var (rel, content) in files)
            _state[Key(destRoot, rel)] = content;

        Applies.Add(new RecordedApply(destRoot, new Dictionary<string, string>(files)));

        return Task.FromResult(new ManagedFileApplyResult
        {
            Succeeded = true,
            Rollback = _ =>
            {
                RollbackCalls++;
                foreach (var (key, prior) in snapshot)
                {
                    if (prior is null) _state.Remove(key);
                    else _state[key] = prior;
                }
                return Task.CompletedTask;
            }
        });
    }

    /// <summary>Reads current content, or null when absent.</summary>
    public string? Read(string destRoot, string relativePath) =>
        _state.TryGetValue(Key(destRoot, relativePath), out var v) ? v : null;

    private static string Key(string destRoot, string relativePath) =>
        destRoot.TrimEnd('/', '\\') + "/" + relativePath.Replace('\\', '/').TrimStart('/');
}
