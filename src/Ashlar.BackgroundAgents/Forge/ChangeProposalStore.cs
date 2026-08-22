using System.Text.Json;

namespace Ashlar.BackgroundAgents.Forge;

/// <summary>
/// Filesystem-backed <see cref="IChangeProposalStore"/>. Each proposal lives
/// as a single JSON file; status changes are file moves between sibling
/// folders so an operator can sift the queue with <c>ls</c>:
///
/// <code>
/// .ashlar/runtime-studio/forge/
///     proposed/    abc123.json
///     approved/    def456.json
///     rejected/    ...
///     applied/     ...
///     stale/       ...
/// </code>
///
/// <para>The store uses <see cref="File.Move(string, string)"/> for transitions
/// rather than rewriting JSON in place — moves are atomic on most filesystems
/// and they sidestep the half-written-file failure mode entirely. JSON is only
/// rewritten when metadata changes (resolver, note, updated_at).</para>
/// </summary>
public sealed class ChangeProposalStore : IChangeProposalStore
{
    private readonly string _root;
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ChangeProposalStore() : this(DefaultRoot()) { }

    public ChangeProposalStore(string root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        EnsureFolders();
    }

    public string Location => _root;

    private static string DefaultRoot()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ASHLAR_FORGE_ROOT");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return Path.GetFullPath(fromEnv);
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".ashlar", "runtime-studio", "forge"));
    }

    private void EnsureFolders()
    {
        Directory.CreateDirectory(_root);
        foreach (var status in Enum.GetValues<ChangeProposalStatus>())
            Directory.CreateDirectory(Path.Combine(_root, status.ToString().ToLowerInvariant()));
    }

    public IReadOnlyList<ChangeProposal> List(ChangeProposalStatus? status = null)
    {
        lock (_gate)
        {
            IEnumerable<ChangeProposalStatus> statuses = status is null
                ? Enum.GetValues<ChangeProposalStatus>()
                : new[] { status.Value };

            var rows = new List<ChangeProposal>();
            foreach (var s in statuses)
            {
                var dir = Path.Combine(_root, s.ToString().ToLowerInvariant());
                if (!Directory.Exists(dir)) continue;
                foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
                {
                    var doc = TryLoad(path);
                    if (doc is not null) rows.Add(doc);
                }
            }
            return rows;
        }
    }

    public ChangeProposal? Find(string id)
    {
        lock (_gate)
        {
            foreach (var s in Enum.GetValues<ChangeProposalStatus>())
            {
                var path = PathFor(id, s);
                if (File.Exists(path)) return TryLoad(path);
            }
            return null;
        }
    }

    public ChangeProposal Add(ChangeProposal proposal)
    {
        if (proposal is null) throw new ArgumentNullException(nameof(proposal));
        if (string.IsNullOrWhiteSpace(proposal.Id))
            throw new ArgumentException("Proposal id required.", nameof(proposal));

        lock (_gate)
        {
            var stamped = proposal with
            {
                Status = ChangeProposalStatus.Proposed,
                CreatedAt = proposal.CreatedAt == default ? DateTimeOffset.UtcNow : proposal.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var path = PathFor(stamped.Id, ChangeProposalStatus.Proposed);
            File.WriteAllText(path, JsonSerializer.Serialize(stamped, Json));
            return stamped;
        }
    }

    public ChangeProposal Approve(string id, string? approver = null, string? note = null)
        => Transition(id, ChangeProposalStatus.Proposed, ChangeProposalStatus.Approved, approver, note);

    public ChangeProposal Reject(string id, string? reviewer = null, string? note = null)
        => Transition(id, ChangeProposalStatus.Proposed, ChangeProposalStatus.Rejected, reviewer, note);

    public ChangeProposal MarkApplied(string id, string? note = null)
        => Transition(id, ChangeProposalStatus.Approved, ChangeProposalStatus.Applied, resolver: null, note: note);

    public ChangeProposal MarkStale(string id, string? note = null)
    {
        // Stale can come from either Proposed or Approved (we don't want to
        // hold an unfulfilled approved proposal forever either).
        lock (_gate)
        {
            var current = Find(id) ?? throw new InvalidOperationException($"Proposal '{id}' not found.");
            if (current.Status is ChangeProposalStatus.Stale or ChangeProposalStatus.Rejected
                or ChangeProposalStatus.Applied)
            {
                return current; // No-op for terminal states.
            }
            var src = PathFor(id, current.Status);
            var dst = PathFor(id, ChangeProposalStatus.Stale);
            var updated = current with
            {
                Status = ChangeProposalStatus.Stale,
                ResolutionNote = note ?? current.ResolutionNote,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            File.WriteAllText(src, JsonSerializer.Serialize(updated, Json));
            File.Move(src, dst, overwrite: true);
            return updated;
        }
    }

    private ChangeProposal Transition(
        string id,
        ChangeProposalStatus expected,
        ChangeProposalStatus next,
        string? resolver,
        string? note)
    {
        lock (_gate)
        {
            var current = Find(id) ?? throw new InvalidOperationException($"Proposal '{id}' not found.");
            if (current.Status != expected)
                throw new InvalidOperationException(
                    $"Proposal '{id}' is {current.Status}; expected {expected} for transition to {next}.");

            var src = PathFor(id, current.Status);
            var dst = PathFor(id, next);
            var updated = current with
            {
                Status = next,
                ResolvedBy = resolver ?? current.ResolvedBy,
                ResolutionNote = note ?? current.ResolutionNote,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            File.WriteAllText(src, JsonSerializer.Serialize(updated, Json));
            File.Move(src, dst, overwrite: true);
            return updated;
        }
    }

    private string PathFor(string id, ChangeProposalStatus status) =>
        Path.Combine(_root, status.ToString().ToLowerInvariant(), id + ".json");

    private static ChangeProposal? TryLoad(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ChangeProposal>(json, Json);
        }
        catch
        {
            // Corrupt JSON shouldn't take down the whole listing — log-by-skip.
            return null;
        }
    }
}
