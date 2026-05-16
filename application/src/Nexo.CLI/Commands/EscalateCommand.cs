using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.Conflicts;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for managing escalations and conflicts.
/// 
/// Provides the `nexo escalate` command with subcommands:
/// - list: List all pending escalations
/// - show: Show details for a specific escalation
/// - resolve: Resolve an escalation with optional resolution description
/// - dismiss: Dismiss an escalation with optional reason
/// - list-by-severity: Filter escalations by severity level
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class EscalateCommand
{
    private readonly EscalationManager _escalationManager;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<EscalateCommand> _logger;

    public EscalateCommand(
        EscalationManager escalationManager,
        IConsoleRenderer renderer,
        ILogger<EscalateCommand> logger)
    {
        _escalationManager = escalationManager ?? throw new ArgumentNullException(nameof(escalationManager));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all pending escalations.
    /// </summary>
    public Task<int> ListAsync(bool json, bool verbose)
    {
        var escalations = _escalationManager.GetPendingEscalations();

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                count = escalations.Count,
                escalations = escalations.Select(e => new
                {
                    id = e.Id,
                    severity = e.Severity.ToString(),
                    status = e.Status.ToString(),
                    escalatedAt = e.EscalatedAt,
                    conflictType = e.Conflict?.ConflictType.ToString(),
                    agentIds = e.Conflict?.AgentIds ?? Array.Empty<string>(),
                    description = e.Description ?? e.Conflict?.Description,
                    context = e.Context
                })
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderEscalations(escalations, verbose);
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Shows details for a specific escalation.
    /// </summary>
    public Task<int> ShowAsync(string escalationId, bool json, bool verbose)
    {
        var allEscalations = _escalationManager.GetAllEscalations();
        var escalation = allEscalations.FirstOrDefault(e => e.Id == escalationId);

        if (escalation == null)
        {
            _renderer.RenderError($"Escalation '{escalationId}' not found");
            return Task.FromResult((int)ExitCode.ValidationFailed);
        }

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                escalation = new
                {
                    id = escalation.Id,
                    severity = escalation.Severity.ToString(),
                    status = escalation.Status.ToString(),
                    escalatedAt = escalation.EscalatedAt,
                    resolvedAt = escalation.ResolvedAt,
                    resolution = escalation.Resolution,
                    conflict = escalation.Conflict != null ? new
                    {
                        type = escalation.Conflict.ConflictType.ToString(),
                        agentIds = escalation.Conflict.AgentIds,
                        description = escalation.Conflict.Description,
                        severity = escalation.Conflict.Severity.ToString()
                    } : null,
                    issueType = escalation.IssueType,
                    description = escalation.Description,
                    context = escalation.Context
                }
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderEscalationDetails(escalation, verbose);
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Resolves an escalation.
    /// </summary>
    public Task<int> ResolveAsync(string escalationId, string? resolution, bool json, bool verbose)
    {
        var allEscalations = _escalationManager.GetAllEscalations();
        var escalation = allEscalations.FirstOrDefault(e => e.Id == escalationId);

        if (escalation == null)
        {
            _renderer.RenderError($"Escalation '{escalationId}' not found");
            return Task.FromResult((int)ExitCode.ValidationFailed);
        }

        if (escalation.Status != EscalationStatus.Pending && escalation.Status != EscalationStatus.InProgress)
        {
            _renderer.RenderError($"Escalation '{escalationId}' is already {escalation.Status}");
            return Task.FromResult((int)ExitCode.ValidationFailed);
        }

        _escalationManager.ResolveEscalation(escalationId, resolution);

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                message = $"Escalation '{escalationId}' resolved",
                escalationId
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderSuccess($"Escalation '{escalationId}' resolved successfully");
            if (!string.IsNullOrWhiteSpace(resolution))
            {
                Console.Out.WriteLine($"Resolution: {resolution}");
            }
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Dismisses an escalation (marks as dismissed without resolution).
    /// </summary>
    public Task<int> DismissAsync(string escalationId, string? reason, bool json, bool verbose)
    {
        var allEscalations = _escalationManager.GetAllEscalations();
        var escalation = allEscalations.FirstOrDefault(e => e.Id == escalationId);

        if (escalation == null)
        {
            _renderer.RenderError($"Escalation '{escalationId}' not found");
            return Task.FromResult((int)ExitCode.ValidationFailed);
        }

        escalation.Status = EscalationStatus.Dismissed;
        escalation.Resolution = reason ?? "Dismissed by operator";

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                message = $"Escalation '{escalationId}' dismissed",
                escalationId
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderSuccess($"Escalation '{escalationId}' dismissed");
            if (!string.IsNullOrWhiteSpace(reason))
            {
                Console.Out.WriteLine($"Reason: {reason}");
            }
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Lists escalations filtered by severity.
    /// </summary>
    public Task<int> ListBySeverityAsync(string severity, bool json, bool verbose)
    {
        if (!Enum.TryParse<EscalationSeverity>(severity, ignoreCase: true, out var severityEnum))
        {
            _renderer.RenderError($"Invalid severity '{severity}'. Valid values: Low, Medium, High, Critical");
            return Task.FromResult((int)ExitCode.ValidationFailed);
        }

        var escalations = _escalationManager.GetEscalationsBySeverity(severityEnum);

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                severity = severityEnum.ToString(),
                count = escalations.Count,
                escalations = escalations.Select(e => new
                {
                    id = e.Id,
                    status = e.Status.ToString(),
                    escalatedAt = e.EscalatedAt,
                    conflictType = e.Conflict?.ConflictType.ToString(),
                    agentIds = e.Conflict?.AgentIds ?? Array.Empty<string>(),
                    description = e.Description ?? e.Conflict?.Description
                })
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            Console.Out.WriteLine($"Escalations with severity '{severityEnum}':");
            _renderer.RenderEscalations(escalations, verbose);
        }

        return Task.FromResult((int)ExitCode.Ok);
    }
}

