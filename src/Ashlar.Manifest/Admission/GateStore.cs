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

    /// <summary>Base64 Ed25519 signature over the canonical record with the two signature
    /// fields null (SPEC-006 §4). Null when the record was written without keys — and a
    /// renderer MUST NOT print a fingerprint for a null sig (rule S-3).</summary>
    public string? Sig { get; init; }

    /// <summary>Base64 raw public key of the signer; null when unsigned.</summary>
    public string? Signer { get; init; }
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
public sealed partial class GateStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _dir;
    private readonly Signing.SigningIdentity? _signer;

    /// <summary>Creates a store rooted at <paramref name="stateRoot"/>/gates. When a
    /// <paramref name="signer"/> is supplied, every record written is signed (SPEC-006);
    /// without one, records are written unsigned — presence-activated, never half-on.</summary>
    public GateStore(string stateRoot, Signing.SigningIdentity? signer = null)
    {
        if (string.IsNullOrWhiteSpace(stateRoot))
        {
            throw new ArgumentException("A state root is required.", nameof(stateRoot));
        }
        _dir = Path.Combine(stateRoot, "gates");
        _signer = signer;
        Directory.CreateDirectory(_dir);
    }

    /// <summary>
    /// Serializes every read-check-write against the store, ACROSS PROCESSES. The lock is a
    /// FileShare.None handle on a well-known file: the OS enforces exclusivity and releases
    /// it when the holder dies, so a crash cannot leave a stale lock. Without this, two
    /// humans could decide the same held proposal and both "win" — an admit silently
    /// erasing a refusal — and two racing self-extending proposals could both read a spent
    /// budget of zero and both admit. On an admission boundary those are security bugs.
    /// </summary>
    private async Task<FileStream> AcquireLockAsync(CancellationToken ct)
    {
        var lockPath = Path.Combine(_dir, ".lock");
        var deadline = Environment.TickCount64 + 15_000;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var handle = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                SweepStrayTmp();
                return handle;
            }
            catch (IOException) when (Environment.TickCount64 < deadline)
            {
                await Task.Delay(25, ct).ConfigureAwait(false);
            }
            catch (IOException)
            {
                throw new TimeoutException(
                    "Could not acquire the gate-store lock within 15s. Another process is holding it unusually long.");
            }
        }
    }

    /// <summary>
    /// A crash between write and move leaves a stray <c>.json.tmp</c>. It can never be
    /// mistaken for a record (listings enumerate <c>*.json</c>, and the extension is four
    /// characters so the Windows legacy three-char pattern quirk does not apply) — but it
    /// would sit there forever. Swept here, under the lock, where no writer can be mid-move.
    /// </summary>
    private void SweepStrayTmp()
    {
        foreach (var stray in Directory.EnumerateFiles(_dir, "*.json.tmp"))
        {
            try
            {
                File.Delete(stray);
            }
            catch (IOException)
            {
                // A stray we cannot delete right now is swept on a later acquisition.
            }
        }
    }

    /// <summary>
    /// Records the gate's automatic outcome for an evaluated proposal. Refuses to overwrite
    /// an existing record — a proposal is recorded once, and the check holds under
    /// concurrency because it runs inside the store lock.
    /// </summary>
    public async Task<GateRecord> RecordAsync(ExtensionProposal proposal, AdmissionOutcome outcome, DateTimeOffset now, CancellationToken ct = default)
    {
        using var _ = await AcquireLockAsync(ct).ConfigureAwait(false);
        return await RecordLockedAsync(proposal, outcome, now, ct).ConfigureAwait(false);
    }

    private async Task<GateRecord> RecordLockedAsync(ExtensionProposal proposal, AdmissionOutcome outcome, DateTimeOffset now, CancellationToken ct)
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
    /// The full propose transaction — count admissions in the window, decide under the
    /// policy, record — as ONE atomic step under the store lock. This lives here rather
    /// than in callers precisely so the budget check and the recording cannot be separated
    /// by another process's admission: budget 1 admits one, under any concurrency.
    /// An unparseable budget window fails closed to Held — never an unlimited allowance.
    /// </summary>
    public async Task<GateRecord> ProposeAsync(AshlarPolicy policy, ExtensionProposal proposal, DateTimeOffset now, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(proposal);

        using var _ = await AcquireLockAsync(ct).ConfigureAwait(false);

        AdmissionOutcome outcome;
        if (policy.SelfExtend.Mode == SelfExtendMode.SelfExtending
            && !AdmissionGate.TryParseWindow(policy.SelfExtend.Budget.Window, out var window))
        {
            outcome = new AdmissionOutcome
            {
                State = ProposalState.Held,
                Reason = $"budget window '{policy.SelfExtend.Budget.Window}' is unparseable — failing closed to a "
                       + "human decision rather than treating it as unlimited.",
            };
        }
        else
        {
            var admittedInWindow = 0;
            if (AdmissionGate.TryParseWindow(policy.SelfExtend.Budget.Window, out var w))
            {
                admittedInWindow = await AdmittedInWindowAsync(w, now, ct).ConfigureAwait(false);
            }
            outcome = AdmissionGate.Decide(policy, proposal, admittedInWindow);
        }

        return await RecordLockedAsync(proposal, outcome, now, ct).ConfigureAwait(false);
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

        using var _ = await AcquireLockAsync(ct).ConfigureAwait(false);

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

    /// <summary>Fetches one record, or null when absent. A file that exists but cannot be
    /// read as a record is an error, never a null.</summary>
    public async Task<GateRecord?> GetAsync(string proposalId, CancellationToken ct = default)
    {
        var path = PathFor(proposalId);
        if (!File.Exists(path))
        {
            return null;
        }
        return await ReadRecordAsync(path, ct).ConfigureAwait(false);
    }

    /// <summary>Lists records, newest first, optionally filtered by state.</summary>
    public async Task<IReadOnlyList<GateRecord>> ListAsync(ProposalState? state = null, CancellationToken ct = default)
    {
        var records = new List<GateRecord>();
        foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
        {
            var record = await ReadRecordAsync(file, ct).ConfigureAwait(false);
            if (state is null || record.State == state)
            {
                records.Add(record);
            }
        }
        return records.OrderByDescending(r => r.Proposal.ProposedAt).ToList();
    }

    /// <summary>
    /// Reads one record file, FAIL-CLOSED. This used to skip records that deserialized to
    /// null and let JsonException escape raw — and a corrupt HELD record silently vanishing
    /// from the queue is an invisible pending decision, the worst possible failure shape
    /// for an admission store. A store this class cannot fully read is a store it refuses
    /// to summarize.
    /// </summary>
    private static async Task<GateRecord> ReadRecordAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var record = await JsonSerializer.DeserializeAsync<GateRecord>(stream, Json, ct).ConfigureAwait(false);
            if (record is null)
            {
                throw new InvalidOperationException(
                    $"Corrupt gate record: {Path.GetFileName(path)} contains no record. "
                    + "Refusing to operate on a store that cannot be fully read — inspect or remove the file.");
            }
            // SPEC-006 rule S-1: a record carrying a signature that does not verify is
            // corrupt — same loud fail-closed path as truncation, because a forged verdict
            // is worse than a missing one.
            if (record.Sig is not null)
            {
                var unsigned = record with { Sig = null, Signer = null };
                if (record.Signer is null
                    || !Signing.OperatorKey.Verify(record.Signer, Signing.CanonicalJson.Bytes(unsigned), record.Sig))
                {
                    throw new InvalidOperationException(
                        $"Corrupt gate record: {Path.GetFileName(path)} carries a signature that does not verify. "
                        + "Refusing to operate — a forged verdict is worse than a missing one.");
                }
            }
            return record;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Corrupt gate record: {Path.GetFileName(path)} is not valid JSON ({ex.Message}). "
                + "Refusing to operate on a store that cannot be fully read — inspect or remove the file.");
        }
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
        // ALLOWLIST, not blocklist. The old check blocked '/', '\' and '.' — and admitted
        // the entire Windows hazard alphabet: reserved names (CON, NUL), trailing dots and
        // spaces (Win32 strips them silently, so 'ext' and 'ext ' collide and append-once
        // is bypassed), ':' (NTFS alternate data streams), and unicode confusables. Ids are
        // machine-generated in this system; there is no reason to accept anything beyond
        // this shape, so nothing beyond it is accepted.
        if (!IdShape().IsMatch(proposalId))
        {
            throw new ArgumentException(
                $"Illegal proposal id '{proposalId}'. Ids are 1-64 characters of [A-Za-z0-9_-], starting alphanumeric.",
                nameof(proposalId));
        }
        // Win32 reserved device names are perfectly alphanumeric, so the shape check passes
        // them — and they are denied on EVERY OS, not just Windows: a store written on Linux
        // with a CON.json breaks the moment it syncs to a Windows machine. Portability means
        // the same ids are legal everywhere.
        if (Win32Reserved.Contains(proposalId))
        {
            throw new ArgumentException(
                $"Illegal proposal id '{proposalId}': a Win32 reserved device name cannot be a store filename on any OS.",
                nameof(proposalId));
        }
        return Path.Combine(_dir, proposalId + ".json");
    }

    private static readonly HashSet<string> Win32Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$")]
    private static partial System.Text.RegularExpressions.Regex IdShape();

    private async Task WriteAsync(string path, GateRecord record, CancellationToken ct)
    {
        // SPEC-006, applied SYMMETRICALLY: always start from the unsigned form, then sign
        // it iff a key is present. The stripping is not optional on the keyless path — a
        // record that was read, mutated, and is now being rewritten (DecideAsync: Held →
        // Admitted) still carries the signature that covered its PRE-mutation content. If a
        // keyless store wrote it back verbatim, that stale signature would cover the wrong
        // bytes, and the very next fail-closed read would reject a legitimate verdict as
        // forged — bricking not just that record but every ListAsync over the store. The
        // rule is absolute: never persist a signature we did not just compute over exactly
        // these bytes. No key ⇒ no signature (S-2), never a half-signed inheritance.
        var unsigned = record with { Sig = null, Signer = null };
        record = _signer is null
            ? unsigned
            : unsigned with
            {
                Sig = _signer.Sign(Signing.CanonicalJson.Bytes(unsigned)),
                Signer = _signer.PublicKeyBase64,
            };

        // Write-then-move so a crash mid-write never leaves a truncated record.
        var tmp = path + ".tmp";
        await using (var stream = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(stream, record, Json, ct).ConfigureAwait(false);
        }
        File.Move(tmp, path, overwrite: true);
    }
}
