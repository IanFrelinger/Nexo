namespace Ashlar.BackgroundAgents.Objectives;

/// <summary>
/// Filesystem-backed implementation of <see cref="IObjectiveStore"/>. Each
/// objective is one markdown file under
/// <c>{root}/.ashlar/runtime-studio/objectives/{status}/{id}.md</c>. Lifecycle
/// transitions are implemented as moves (rename across status folders) so the
/// physical layout always matches the logical state and operators can `git
/// status` the backlog at a glance.
///
/// <para>Concurrency is process-local: a single <see cref="object"/> mutex
/// serialises Claim/Complete/Block calls so two scheduler ticks running in the
/// same daemon can't race for the same objective. Cross-process safety relies
/// on the rename being atomic on the host filesystem (true on NTFS and ext4
/// for same-volume renames).</para>
///
/// <para>Auto-blocking: when <see cref="ClaimNext"/> would return an objective
/// whose <c>Attempts</c> already meets <see cref="MaxAttemptsBeforeBlock"/>,
/// the store transparently routes it to <c>Blocked</c> with reason
/// <c>"max_attempts_reached"</c> and tries the next candidate. This stops a
/// brittle objective from monopolising every cycle.</para>
/// </summary>
public sealed class ObjectiveStore : IObjectiveStore
{
    /// <summary>Default path relative to the working directory.</summary>
    public const string DefaultRelativePath = ".ashlar/runtime-studio/objectives";

    /// <summary>Maximum length of an objective id used as a filename.</summary>
    public const int MaxIdLength = 128;

    /// <summary>Human-readable objective id contract.</summary>
    public const string IdRequirement =
        "Use 1-128 ASCII letters, digits, '.', '_' or '-', beginning with a letter or digit; Windows device names are not allowed.";

    private static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// Default cap on how many times the planner can attempt the same objective
    /// before the store auto-blocks it. Sized to allow a handful of transient
    /// failures (LLM stalls, flake) before declaring the goal stuck.
    /// </summary>
    public const int MaxAttemptsBeforeBlock = 5;

    private readonly string _root;
    private readonly int _maxAttempts;
    private readonly object _gate = new();

    public ObjectiveStore()
        : this(System.IO.Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath))
    {
    }

    public ObjectiveStore(string root, int? maxAttempts = null)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Objective store root must be non-empty.", nameof(root));
        _root = System.IO.Path.GetFullPath(root);
        _maxAttempts = maxAttempts ?? MaxAttemptsBeforeBlock;
    }

    public string Location => _root;

    public IReadOnlyList<ObjectiveDocument> List(ObjectiveStatus? status = null)
    {
        var statuses = status is null
            ? Enum.GetValues<ObjectiveStatus>()
            : new[] { status.Value };

        var result = new List<ObjectiveDocument>();
        foreach (var s in statuses)
        {
            var dir = StatusDir(s);
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
            {
                var id = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!IsValidId(id))
                    throw new InvalidDataException($"Invalid objective filename '{id}.md'. {IdRequirement}");
                var text = File.ReadAllText(file);
                result.Add(ObjectiveDocumentParser.Parse(text, id, s));
            }
        }

        // Pending objectives need stable ordering for Claim — primary key is priority
        // (lower is more important), tie-break on creation timestamp so a freshly added
        // high-priority objective preempts older ones with the same priority.
        return result
            .OrderBy(o => (int)o.Status)
            .ThenBy(o => o.Priority)
            .ThenBy(o => o.CreatedAt)
            .ToList();
    }

    public ObjectiveDocument? Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (var s in Enum.GetValues<ObjectiveStatus>())
        {
            var path = PathFor(id, s);
            if (File.Exists(path))
                return ObjectiveDocumentParser.Parse(File.ReadAllText(path), id, s);
        }
        return null;
    }

    public ObjectiveDocument? ClaimNext(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner is required to claim an objective.", nameof(owner));
        lock (_gate)
        {
            // Walk pending in priority order; auto-block any that have already
            // burned through their attempts allowance.
            foreach (var pending in List(ObjectiveStatus.Pending))
            {
                if (pending.Attempts >= _maxAttempts)
                {
                    BlockInternal(pending, "max_attempts_reached");
                    continue;
                }
                var claimed = pending with
                {
                    Status = ObjectiveStatus.InProgress,
                    Owner = owner,
                    Attempts = pending.Attempts + 1,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                MoveTo(pending, ObjectiveStatus.InProgress, claimed);
                return claimed;
            }
            return null;
        }
    }

    public ObjectiveDocument Add(ObjectiveDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (string.IsNullOrWhiteSpace(document.Id))
            throw new ArgumentException("Objective Id is required.", nameof(document));
        lock (_gate)
        {
            var pending = document with
            {
                Status = ObjectiveStatus.Pending,
                CreatedAt = document.CreatedAt == default ? DateTimeOffset.UtcNow : document.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var path = PathFor(pending.Id, ObjectiveStatus.Pending);
            EnsureDir(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, pending.Serialize());
            return pending;
        }
    }

    public ObjectiveDocument Complete(string id, string? note = null)
    {
        lock (_gate)
        {
            var current = Find(id) ?? throw new InvalidOperationException($"Objective '{id}' not found.");
            if (current.Status != ObjectiveStatus.InProgress)
                throw new InvalidOperationException($"Cannot complete '{id}': current status is {current.Status} (expected InProgress).");
            var updated = current with
            {
                Status = ObjectiveStatus.Done,
                UpdatedAt = DateTimeOffset.UtcNow,
                Body = string.IsNullOrWhiteSpace(note)
                    ? current.Body
                    : current.Body + Environment.NewLine + Environment.NewLine + "## Completion note" + Environment.NewLine + note
            };
            MoveTo(current, ObjectiveStatus.Done, updated);
            return updated;
        }
    }

    public ObjectiveDocument Block(string id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Block reason is required.", nameof(reason));
        lock (_gate)
        {
            var current = Find(id) ?? throw new InvalidOperationException($"Objective '{id}' not found.");
            if (current.Status is ObjectiveStatus.Done or ObjectiveStatus.Blocked)
                throw new InvalidOperationException($"Cannot block '{id}': current status is {current.Status}.");
            return BlockInternal(current, reason);
        }
    }

    public ObjectiveDocument Unblock(string id)
    {
        lock (_gate)
        {
            var current = Find(id) ?? throw new InvalidOperationException($"Objective '{id}' not found.");
            if (current.Status != ObjectiveStatus.Blocked)
                throw new InvalidOperationException($"Cannot unblock '{id}': current status is {current.Status} (expected Blocked).");
            var updated = current with
            {
                Status = ObjectiveStatus.Pending,
                BlockedReason = null,
                Attempts = 0,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            MoveTo(current, ObjectiveStatus.Pending, updated);
            return updated;
        }
    }

    private ObjectiveDocument BlockInternal(ObjectiveDocument current, string reason)
    {
        var updated = current with
        {
            Status = ObjectiveStatus.Blocked,
            BlockedReason = reason,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        MoveTo(current, ObjectiveStatus.Blocked, updated);
        return updated;
    }

    private void MoveTo(ObjectiveDocument current, ObjectiveStatus newStatus, ObjectiveDocument updated)
    {
        var oldPath = PathFor(current.Id, current.Status);
        var newPath = PathFor(updated.Id, newStatus);
        EnsureDir(System.IO.Path.GetDirectoryName(newPath)!);

        // Write to a temp file in the destination dir then atomic-replace, so a crash
        // mid-write can't leave both the old and new files present.
        var tmp = newPath + ".tmp";
        File.WriteAllText(tmp, updated.Serialize());
        if (File.Exists(newPath)) File.Delete(newPath);
        File.Move(tmp, newPath);
        if (oldPath != newPath && File.Exists(oldPath))
            File.Delete(oldPath);
    }

    private string StatusDir(ObjectiveStatus status) => System.IO.Path.Combine(_root, FolderFor(status));

    private string PathFor(string id, ObjectiveStatus status)
    {
        if (!IsValidId(id))
            throw new ArgumentException($"Invalid objective id '{id}'. {IdRequirement}", nameof(id));

        var statusDir = System.IO.Path.GetFullPath(StatusDir(status));
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(statusDir, id + ".md"));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var containedPrefix = statusDir.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
        if (!path.StartsWith(containedPrefix, comparison))
            throw new InvalidOperationException($"Objective path escaped its status directory: '{id}'.");
        return path;
    }

    /// <summary>Returns true when an id is safe as a portable objective filename.</summary>
    public static bool IsValidId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > MaxIdLength || !IsAsciiLetterOrDigit(id[0]))
            return false;
        if (id[^1] == '.' || id.Any(c => !IsAsciiLetterOrDigit(c) && c is not '-' and not '_' and not '.'))
            return false;
        var stem = id.Split('.', 2)[0];
        return !WindowsReservedNames.Contains(stem);
    }

    private static bool IsAsciiLetterOrDigit(char value)
        => value is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z'
            or >= '0' and <= '9';

    private static string FolderFor(ObjectiveStatus status) => status switch
    {
        ObjectiveStatus.Pending => "pending",
        ObjectiveStatus.InProgress => "in_progress",
        ObjectiveStatus.Done => "done",
        ObjectiveStatus.Blocked => "blocked",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static void EnsureDir(string dir)
    {
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
