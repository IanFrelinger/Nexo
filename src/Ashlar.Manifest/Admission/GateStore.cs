using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ashlar.Manifest.Admission;

/// <summary>A proposal with its current state and decision history, as persisted.</summary>
public sealed record GateRecord
{
    /// <summary>The proposal.</summary>
    public required ExtensionProposal Proposal { get; init; }

    /// <summary>Current state.</summary>
    public required ProposalState State { get; init; }

    /// <summary>Why it is in that state.</summary>
    public required string Reason { get; init; }

    /// <summary>Who put it there: <c>gate</c> for automatic outcomes, otherwise the human's id.</summary>
    public required string Actor { get; init; }

    /// <summary>When the state was last decided (UTC).</summary>
    public required DateTimeOffset DecidedAt { get; init; }
}

/// <summary>
/// Durable, file-backed store for gate records — one JSON file per proposal under
/// <c>{root}/gates/</c>.
///
/// <para>Two rules live here rather than in callers. First, DURABILITY: a held proposal
/// must survive process death, because the reviewer is asleep when the app proposes — this
/// codebase's audit found process-lifetime state at nearly every point a durable record was
/// needed, and this store is the convention that answers it. Second, TRANSITION AUTHORITY
/// (SPEC-004): <see cref="DecideAsync"/> moves a proposal out of Held and nothing else —
/// admitted and rejected records are immutable history, so there is no way to re-decide a
/// refusal or quietly edit an admission, including for the vendor.</para>
/// </summary>
public sealed class GateStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _dir;

    /// <summary>Creates a store rooted at <paramref name="stateRoot"/>/gates.</summary>
    public GateStore(string stateRoot)
    {
        if (string.IsNullOrWhiteSpace(stateRoot))
        {
            throw new ArgumentException("A state root is required.", nameof(stateRoot));
        }
        _dir = Path.Combine(stateRoot, "gates");
        Directory.CreateDirectory(_dir);
    }

    /// <summary>
    /// Records the gate's automatic outcome for an evaluated proposal. Refuses to overwrite
    /// an existing record — a proposal is recorded once.
    /// </summary>
    public async Task<GateRecord> RecordAsync(ExtensionProposal proposal, AdmissionOutcome outcome, DateTimeOffset now, CancellationToken ct = default)
    {
        var path = PathFor(proposal.Id);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"Proposal '{proposal.Id}' is already recorded. Records are append-once; propose under a new id.");
        }

        var record = new GateRecord
        {
            Proposal = proposal,
            State = outcome.State,
            Reason = outcome.Reason,
            Actor = "gate",
            DecidedAt = now,
        };
        await WriteAsync(path, record, ct).ConfigureAwait(false);
        return record;
    }

    /// <summary>
    /// A human decides a HELD proposal. The only legal transitions are Held → Admitted and
    /// Held → Refused; anything else is refused with the rule spelled out. A refusal
    /// requires a reason — a refusal that does not teach produces the same proposal again.
    /// </summary>
    public async Task<GateRecord> DecideAsync(string proposalId, bool admit, string actor, string reason, DateTimeOffset now, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("A decision needs an actor: the record must say who seated or refused.", nameof(actor));
        }
        if (!admit && string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A refusal requires a reason — it is recorded and fed back to the proposer.", nameof(reason));
        }

        var existing = await GetAsync(proposalId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No proposal '{proposalId}' in the store.");

        if (existing.State != ProposalState.Held)
        {
            throw new InvalidOperationException(
                $"Proposal '{proposalId}' is {existing.State}, not Held. Only held proposals can be decided: "
                + "admitted and rejected records are immutable history (SPEC-004 — no administrative path).");
        }

        var decided = existing with
        {
            State = admit ? ProposalState.Admitted : ProposalState.Refused,
            Reason = string.IsNullOrWhiteSpace(reason) ? "seated by operator" : reason,
            Actor = actor,
            DecidedAt = now,
        };
        await WriteAsync(PathFor(proposalId), decided, ct).ConfigureAwait(false);
        return decided;
    }

    /// <summary>Fetches one record, or null.</summary>
    public async Task<GateRecord?> GetAsync(string proposalId, CancellationToken ct = default)
    {
        var path = PathFor(proposalId);
        if (!File.Exists(path))
        {
            return null;
        }
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<GateRecord>(stream, Json, ct).ConfigureAwait(false);
    }

    /// <summary>Lists records, newest first, optionally filtered by state.</summary>
    public async Task<IReadOnlyList<GateRecord>> ListAsync(ProposalState? state = null, CancellationToken ct = default)
    {
        var records = new List<GateRecord>();
        foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var record = await JsonSerializer.DeserializeAsync<GateRecord>(stream, Json, ct).ConfigureAwait(false);
            if (record is not null && (state is null || record.State == state))
            {
                records.Add(record);
            }
        }
        return records.OrderByDescending(r => r.Proposal.ProposedAt).ToList();
    }

    /// <summary>
    /// How many extensions were admitted inside the budget window ending now. Drives the
    /// self-extending budget check.
    /// </summary>
    public async Task<int> AdmittedInWindowAsync(TimeSpan window, DateTimeOffset now, CancellationToken ct = default)
    {
        var records = await ListAsync(ProposalState.Admitted, ct).ConfigureAwait(false);
        var cutoff = now - window;
        return records.Count(r => r.DecidedAt >= cutoff);
    }

    private string PathFor(string proposalId)
    {
        // Fail closed on ids that would escape the store directory.
        if (proposalId.Any(c => c == '/' || c == '\\' || c == '.'))
        {
            throw new ArgumentException($"Illegal proposal id '{proposalId}'.", nameof(proposalId));
        }
        return Path.Combine(_dir, proposalId + ".json");
    }

    private static async Task WriteAsync(string path, GateRecord record, CancellationToken ct)
    {
        // Write-then-move so a crash mid-write never leaves a truncated record.
        var tmp = path + ".tmp";
        await using (var stream = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(stream, record, Json, ct).ConfigureAwait(false);
        }
        File.Move(tmp, path, overwrite: true);
    }
}
