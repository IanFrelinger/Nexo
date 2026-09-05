using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.CLI.Commands.Runtime;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// Operator front door for the structured objective backlog. Lets a human
/// list / add / complete / block / unblock / show backlog items without
/// needing the daemon to be running. The store is the same one the
/// background-agent runner reads, so adds made through this command are
/// picked up by the next planner cycle automatically.
///
/// <para>This command is intentionally read-mostly and offline. The two write
/// verbs (<c>add</c>, <c>complete</c>, <c>block</c>, <c>unblock</c>) are
/// guarded by the same store invariants the runtime uses (e.g. you can't
/// complete a Pending item — only InProgress), so an operator can't put the
/// backlog into a state the agent can't recover from.</para>
/// </summary>
public class ObjectivesBackgroundAgentCommand
{
    private readonly IObjectiveStore _store;
    private readonly IObservationStore? _observations;
    private readonly ILogger<ObjectivesBackgroundAgentCommand> _logger;

    /// <summary>Creates the objectives background-agent command handler.</summary>
    public ObjectivesBackgroundAgentCommand(
        IObjectiveStore store,
        ILogger<ObjectivesBackgroundAgentCommand> logger,
        IObservationStore? observations = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _observations = observations;
    }

    /// <summary>
    /// List backlog items, optionally filtered by status (Pending, InProgress,
    /// Done, Blocked) or tag. Output is sorted by status then priority so
    /// "what's next?" is always the first row in a Pending listing.
    /// </summary>
    public Task<int> ListAsync(string? status, string? tag, bool formatJson, CancellationToken ct = default)
        => ListAsync(status, tag, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Lists objectives with optional status and owner filters.</summary>
    public Task<int> ListAsync(
        string? status,
        string? tag,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            ObjectiveStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ObjectiveStatus>(status, ignoreCase: true, out var parsed))
                {
                    var allowed = string.Join(", ", Enum.GetNames<ObjectiveStatus>());
                    var msg = $"Unknown --status '{status}'. Allowed: {allowed}.";
                    if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = msg }));
                    else stderr.WriteLine(msg);
                    return Task.FromResult(2);
                }
                statusFilter = parsed;
            }

            var rows = _store.List(statusFilter)
                .Where(o => string.IsNullOrWhiteSpace(tag) ||
                            o.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(o => (int)o.Status)
                .ThenBy(o => o.Priority)
                .ThenBy(o => o.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new { ok = true, path = _store.Location, count = rows.Count, objectives = rows.Select(Project) },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Source: {_store.Location}");
            if (rows.Count == 0)
            {
                stdout.WriteLine("No matching objectives.");
                return Task.FromResult(0);
            }
            stdout.WriteLine($"Objectives: {rows.Count}");
            stdout.WriteLine();
            stdout.WriteLine("Id                              Status      Pri  Att  Owner               Title");
            stdout.WriteLine("------------------------------- ----------- ---- ---- ------------------- ----------------------------------------");
            foreach (var o in rows)
            {
                stdout.WriteLine(
                    $"{Truncate(o.Id, 31),-31} {o.Status,-11} {o.Priority,4} {o.Attempts,4} {Truncate(o.Owner ?? "-", 19),-19} {Truncate(o.Title, 40)}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("List objectives failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>
    /// Show one objective's full body and metadata, including blocked reason
    /// when applicable. JSON mode round-trips the document back to the caller.
    /// </summary>
    public Task<int> ShowAsync(string id, bool formatJson, CancellationToken ct = default)
        => ShowAsync(id, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Shows a single objective record.</summary>
    public Task<int> ShowAsync(string id, bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var doc = _store.Find(id);
            if (doc is null)
            {
                var msg = $"Objective '{id}' not found.";
                if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = msg }));
                else stderr.WriteLine(msg);
                return Task.FromResult(1);
            }

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new { ok = true, objective = Project(doc) },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Id:         {doc.Id}");
            stdout.WriteLine($"Title:      {doc.Title}");
            stdout.WriteLine($"Status:     {doc.Status}");
            stdout.WriteLine($"Priority:   {doc.Priority}");
            stdout.WriteLine($"Attempts:   {doc.Attempts}");
            if (!string.IsNullOrWhiteSpace(doc.Owner))
                stdout.WriteLine($"Owner:      {doc.Owner}");
            if (doc.Tags.Count > 0)
                stdout.WriteLine($"Tags:       {string.Join(", ", doc.Tags)}");
            if (!string.IsNullOrWhiteSpace(doc.BlockedReason))
                stdout.WriteLine($"Blocked:    {doc.BlockedReason}");
            stdout.WriteLine($"Created:    {doc.CreatedAt:u}");
            stdout.WriteLine($"Updated:    {doc.UpdatedAt:u}");
            stdout.WriteLine();
            stdout.WriteLine(doc.Body.TrimEnd());
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Show objective failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>
    /// Add a new pending objective. The body can be supplied inline via
    /// <paramref name="body"/> or read from a file via <paramref name="bodyFile"/>;
    /// when neither is supplied the body is left empty so the planner has only
    /// the title to go on.
    /// </summary>
    public Task<int> AddAsync(
        string id,
        string title,
        int priority,
        IReadOnlyList<string>? tags,
        string? body,
        string? bodyFile,
        bool formatJson,
        CancellationToken ct = default)
        => AddAsync(id, title, priority, tags, body, bodyFile, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Creates a new AddAsync instance.</summary>
    public Task<int> AddAsync(
        string id,
        string title,
        int priority,
        IReadOnlyList<string>? tags,
        string? body,
        string? bodyFile,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("--id is required", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("--title is required", nameof(title));

            var normalizedId = id.Trim();
            if (!ObjectiveStore.IsValidId(normalizedId))
            {
                var message = $"Invalid --id. {ObjectiveStore.IdRequirement}";
                if (formatJson)
                    stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = message }));
                else
                    stderr.WriteLine(message);
                return Task.FromResult(1);
            }

            var resolvedBody = body ?? string.Empty;
            if (string.IsNullOrEmpty(resolvedBody) && !string.IsNullOrWhiteSpace(bodyFile))
            {
                if (!File.Exists(bodyFile))
                    throw new FileNotFoundException($"--body-file '{bodyFile}' not found.", bodyFile);
                resolvedBody = File.ReadAllText(bodyFile, Encoding.UTF8);
            }

            var now = DateTimeOffset.UtcNow;
            var doc = new ObjectiveDocument
            {
                Id = normalizedId,
                Title = title.Trim(),
                Priority = priority,
                Tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray()
                       ?? Array.Empty<string>(),
                Body = resolvedBody,
                CreatedAt = now,
                UpdatedAt = now
            };

            var saved = _store.Add(doc);
            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new { ok = true, action = "added", objective = Project(saved) },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                stdout.WriteLine($"Added objective '{saved.Id}' (priority {saved.Priority}) to {_store.Location}/pending");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Add objective failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>
    /// Mark an in-progress objective as Done. Mostly used by operators to retire
    /// items the agent finished outside of the normal tool-call path (e.g. they
    /// merged the equivalent change manually).
    /// </summary>
    public Task<int> CompleteAsync(string id, string? note, bool formatJson, CancellationToken ct = default)
        => CompleteAsync(id, note, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Creates a new CompleteAsync instance.</summary>
    public Task<int> CompleteAsync(string id, string? note, bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var saved = _store.Complete(id, note);
            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "completed", objective = Project(saved) }));
            else
                stdout.WriteLine($"Completed objective '{saved.Id}'.");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Complete objective failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>Creates a new BlockAsync instance.</summary>
    public Task<int> BlockAsync(string id, string reason, bool formatJson, CancellationToken ct = default)
        => BlockAsync(id, reason, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Creates a new BlockAsync instance.</summary>
    public Task<int> BlockAsync(string id, string reason, bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("--reason is required", nameof(reason));
            var saved = _store.Block(id, reason);
            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "blocked", objective = Project(saved) }));
            else
                stdout.WriteLine($"Blocked objective '{saved.Id}': {reason}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Block objective failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>Creates a new UnblockAsync instance.</summary>
    public Task<int> UnblockAsync(string id, bool formatJson, CancellationToken ct = default)
        => UnblockAsync(id, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Creates a new UnblockAsync instance.</summary>
    public Task<int> UnblockAsync(string id, bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var saved = _store.Unblock(id);
            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "unblocked", objective = Project(saved) }));
            else
                stdout.WriteLine($"Unblocked objective '{saved.Id}'.");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Unblock objective failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>
    /// Per-status counts plus a per-tag breakdown so an operator can quickly
    /// answer "how big is the pending queue and which themes dominate?".
    /// </summary>
    public Task<int> StatsAsync(bool formatJson, CancellationToken ct = default)
        => StatsAsync(formatJson, Console.Out, Console.Error, ct);

    /// <summary>Reports aggregate objective queue statistics.</summary>
    public Task<int> StatsAsync(bool formatJson, TextWriter stdout, TextWriter stderr, CancellationToken ct = default)
    {
        try
        {
            var all = _store.List();
            var byStatus = Enum.GetValues<ObjectiveStatus>()
                .ToDictionary(s => s.ToString(), s => all.Count(o => o.Status == s));
            var byTag = all
                .SelectMany(o => o.Tags.Select(t => (tag: t, status: o.Status)))
                .GroupBy(x => x.tag, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => new
                {
                    tag = g.Key,
                    total = g.Count(),
                    pending = g.Count(x => x.status == ObjectiveStatus.Pending),
                    in_progress = g.Count(x => x.status == ObjectiveStatus.InProgress),
                    done = g.Count(x => x.status == ObjectiveStatus.Done),
                    blocked = g.Count(x => x.status == ObjectiveStatus.Blocked)
                })
                .ToList();

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new { ok = true, path = _store.Location, total = all.Count, byStatus, byTag },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Source: {_store.Location}");
            stdout.WriteLine($"Total objectives: {all.Count}");
            stdout.WriteLine();
            stdout.WriteLine("Status        Count");
            stdout.WriteLine("------------- ------");
            foreach (var (s, c) in byStatus)
                stdout.WriteLine($"{s,-13} {c,6}");
            if (byTag.Count > 0)
            {
                stdout.WriteLine();
                stdout.WriteLine("Tag                            Total  Pending  InProg  Done  Blocked");
                stdout.WriteLine("------------------------------ ------ -------- ------- ----- -------");
                foreach (var t in byTag)
                {
                    stdout.WriteLine(
                        $"{Truncate(t.tag, 30),-30} {t.total,6} {t.pending,8} {t.in_progress,7} {t.done,5} {t.blocked,7}");
                }
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Objectives stats failed", ex, formatJson, stdout, stderr);
        }
    }

    /// <summary>
    /// Cross-correlate the backlog with the observations log: for each objective
    /// (optionally filtered by status / id), report attempts, total observations
    /// tagged with that <c>objective_id</c>, and a per-kind breakdown. Answers
    /// "what is the planner actually doing for objective X?" without forcing the
    /// operator to grep two files at once.
    ///
    /// <para>Observations are matched by their <c>objective_id</c> fact, which the
    /// runner stamps into <c>dotnet.build</c> / <c>dotnet.test</c> observations for
    /// the duration of an objective's claim. Observations without that fact (e.g.
    /// background tester/optimizer cycles) are silently excluded from the per-objective
    /// rollup since they're not attributable to any single backlog item.</para>
    /// </summary>
    public Task<int> ReportAsync(string? id, string? status, double? sinceHours, bool formatJson, CancellationToken ct = default)
        => ReportAsync(id, status, sinceHours, formatJson, Console.Out, Console.Error, ct);

    /// <summary>Creates a new ReportAsync instance.</summary>
    public Task<int> ReportAsync(
        string? id,
        string? status,
        double? sinceHours,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            if (!RuntimeCommandUtilities.TryValidateOptionalPositiveDuration(sinceHours))
            {
                if (formatJson)
                    stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "Invalid --since-hours" }));
                else
                    stderr.WriteLine(RuntimeCommandUtilities.InvalidSinceHoursMessage);
                return Task.FromResult(1);
            }

            ObjectiveStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ObjectiveStatus>(status, ignoreCase: true, out var parsed))
                {
                    var allowed = string.Join(", ", Enum.GetNames<ObjectiveStatus>());
                    var msg = $"Unknown --status '{status}'. Allowed: {allowed}.";
                    if (formatJson) stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = msg }));
                    else stderr.WriteLine(msg);
                    return Task.FromResult(2);
                }
                statusFilter = parsed;
            }

            var objectives = _store.List(statusFilter)
                .Where(o => string.IsNullOrWhiteSpace(id) ||
                            string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(o => (int)o.Status)
                .ThenBy(o => o.Priority)
                .ThenBy(o => o.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Index observations by objective_id (only those that carry one — others
            // can't be attributed to a backlog item and would inflate the per-objective
            // counts in misleading ways).
            DateTimeOffset? cutoff = sinceHours is > 0
                ? DateTimeOffset.UtcNow - TimeSpan.FromHours(sinceHours.Value)
                : null;
            var byObjective = new Dictionary<string, List<RuntimeObservation>>(StringComparer.OrdinalIgnoreCase);
            if (_observations is not null)
            {
                foreach (var o in _observations.ReadSince(since: cutoff))
                {
                    if (o.facts is null) continue;
                    if (!o.facts.TryGetValue("objective_id", out var oid) || string.IsNullOrWhiteSpace(oid)) continue;
                    if (!byObjective.TryGetValue(oid, out var list))
                    {
                        list = new List<RuntimeObservation>();
                        byObjective[oid] = list;
                    }
                    list.Add(o);
                }
            }

            var rows = objectives.Select(o =>
            {
                var obs = byObjective.TryGetValue(o.Id, out var list) ? list : new List<RuntimeObservation>();
                return new ObjectiveReportRow(
                    o.Id,
                    o.Title,
                    o.Status,
                    o.Priority,
                    o.Attempts,
                    obs.Count,
                    obs.Count(x => x.kind == ObservationKind.Build),
                    obs.Count(x => x.kind == ObservationKind.Test),
                    obs.Count(x => x.kind == ObservationKind.Analysis),
                    obs.Count(x => x.severity == ObservationSeverity.Error),
                    obs.Count == 0 ? (DateTimeOffset?)null : obs.Max(x => x.ts));
            }).ToList();

            if (formatJson)
            {
                stdout.WriteLine(JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        objectivesPath = _store.Location,
                        observationsPath = _observations?.Location,
                        sinceHours,
                        count = rows.Count,
                        rows
                    },
                    new JsonSerializerOptions { WriteIndented = true }));
                return Task.FromResult(0);
            }

            stdout.WriteLine($"Objectives: {_store.Location}");
            if (_observations is not null) stdout.WriteLine($"Observations: {_observations.Location}");
            if (sinceHours is > 0) stdout.WriteLine($"Window: last {sinceHours} hour(s)");
            if (rows.Count == 0)
            {
                stdout.WriteLine("No matching objectives.");
                return Task.FromResult(0);
            }
            stdout.WriteLine();
            stdout.WriteLine("Id                              Status      Pri  Att  Obs  Build  Test  Analysis  Errors  LastObs");
            stdout.WriteLine("------------------------------- ----------- ---- ---- ---- ------ ----- --------- ------- --------------------");
            foreach (var r in rows)
            {
                stdout.WriteLine(
                    $"{Truncate(r.Id, 31),-31} {r.Status,-11} {r.Priority,4} {r.Attempts,4} {r.Observations,4} {r.Build,6} {r.Test,5} {r.Analysis,9} {r.Errors,7} " +
                    (r.LastObservation is null ? "-" : r.LastObservation.Value.ToString("u")));
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            return Fail("Objectives report failed", ex, formatJson, stdout, stderr);
        }
    }

    private sealed record ObjectiveReportRow(
        string Id,
        string Title,
        ObjectiveStatus Status,
        int Priority,
        int Attempts,
        int Observations,
        int Build,
        int Test,
        int Analysis,
        int Errors,
        DateTimeOffset? LastObservation);

    private Task<int> Fail(string label, Exception ex, bool formatJson, TextWriter stdout, TextWriter stderr)
    {
        _logger.LogError(ex, "{Label}", label);
        if (formatJson)
            stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
        else
            stderr.WriteLine(ex.Message);
        return Task.FromResult(1);
    }

    private static object Project(ObjectiveDocument o) => new
    {
        id = o.Id,
        title = o.Title,
        status = o.Status.ToString(),
        owner = o.Owner,
        priority = o.Priority,
        attempts = o.Attempts,
        blockedReason = o.BlockedReason,
        tags = o.Tags,
        createdAt = o.CreatedAt,
        updatedAt = o.UpdatedAt,
        body = o.Body
    };

    private static string Truncate(string s, int len)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= len ? s : s.Substring(0, len - 1) + "…";
    }
}
