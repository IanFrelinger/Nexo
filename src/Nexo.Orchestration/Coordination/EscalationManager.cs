using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Coordination.Conflicts;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Manages escalation of conflicts and issues to human operators.
/// </summary>
public sealed class EscalationManager
{
    private readonly ILogger<EscalationManager> _logger;
    private readonly List<Escalation> _escalations = new();

    public EscalationManager(ILogger<EscalationManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Escalates a conflict to human operators.
    /// </summary>
    public Escalation EscalateConflict(Conflict conflict, string? context = null)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        var escalation = new Escalation
        {
            Id = Guid.NewGuid().ToString(),
            Conflict = conflict,
            EscalatedAt = DateTimeOffset.UtcNow,
            Severity = (EscalationSeverity)(int)conflict.Severity,
            Context = context,
            Status = EscalationStatus.Pending
        };

        _escalations.Add(escalation);

        _logger.LogWarning("Escalated conflict {ConflictType} between agents {AgentIds}: {Description}",
            conflict.ConflictType, string.Join(", ", conflict.AgentIds), conflict.Description);

        return escalation;
    }

    /// <summary>
    /// Escalates a general issue (not a conflict).
    /// </summary>
    public Escalation EscalateIssue(string issueType, string description, EscalationSeverity severity, string? context = null)
    {
        var escalation = new Escalation
        {
            Id = Guid.NewGuid().ToString(),
            IssueType = issueType,
            Description = description,
            EscalatedAt = DateTimeOffset.UtcNow,
            Severity = severity,
            Context = context,
            Status = EscalationStatus.Pending
        };

        _escalations.Add(escalation);

        _logger.LogWarning("Escalated issue {IssueType}: {Description}", issueType, description);

        return escalation;
    }

    /// <summary>
    /// Resolves an escalation (human operator has handled it).
    /// </summary>
    public void ResolveEscalation(string escalationId, string? resolution = null)
    {
        var escalation = _escalations.FirstOrDefault(e => e.Id == escalationId);
        if (escalation == null)
        {
            _logger.LogWarning("Attempted to resolve non-existent escalation {EscalationId}", escalationId);
            return;
        }

        escalation.Status = EscalationStatus.Resolved;
        escalation.ResolvedAt = DateTimeOffset.UtcNow;
        escalation.Resolution = resolution;

        _logger.LogInformation("Resolved escalation {EscalationId}: {Resolution}", escalationId, resolution ?? "No resolution provided");
    }

    /// <summary>
    /// Gets all pending escalations.
    /// </summary>
    public IReadOnlyList<Escalation> GetPendingEscalations()
    {
        return _escalations
            .Where(e => e.Status == EscalationStatus.Pending)
            .OrderByDescending(e => e.Severity)
            .ToList();
    }

    /// <summary>
    /// Gets escalations by severity.
    /// </summary>
    public IReadOnlyList<Escalation> GetEscalationsBySeverity(EscalationSeverity severity)
    {
        return _escalations
            .Where(e => e.Severity == severity)
            .OrderByDescending(e => e.EscalatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets all escalations.
    /// </summary>
    public IReadOnlyList<Escalation> GetAllEscalations()
    {
        return _escalations.ToList();
    }
}

/// <summary>
/// Represents an escalation to human operators.
/// </summary>
public sealed class Escalation
{
    public required string Id { get; init; }
    public Conflict? Conflict { get; init; }
    public string? IssueType { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset EscalatedAt { get; init; }
    public EscalationSeverity Severity { get; init; }
    public string? Context { get; init; }
    public EscalationStatus Status { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}

/// <summary>
/// Severity levels for escalations (maps to ConflictSeverity).
/// </summary>
public enum EscalationSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Status of an escalation.
/// </summary>
public enum EscalationStatus
{
    Pending,
    InProgress,
    Resolved,
    Dismissed
}

