using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;

namespace Nexo.BackgroundAgents.RuntimeStudio;

/// <summary>
/// Shared read-only aggregation for API and CLI dashboards (single source of truth for backlog counts + SLA hints).
/// </summary>
public static class RuntimeStudioMetricsCollector
{
    public static RuntimeStudioDiskMetrics Collect(
        IObjectiveStore objectives,
        IChangeProposalStore proposals,
        string observationsPath,
        DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        var allObjectives = objectives.List(null);
        var objectivesByStatus = Enum.GetValues<ObjectiveStatus>()
            .ToDictionary(s => s.ToString(), s => allObjectives.Count(o => o.Status == s), StringComparer.Ordinal);

        var pending = allObjectives.Where(o => o.Status == ObjectiveStatus.Pending).ToList();
        var inProgress = allObjectives.Where(o => o.Status == ObjectiveStatus.InProgress).ToList();
        var blocked = allObjectives.Where(o => o.Status == ObjectiveStatus.Blocked).ToList();

        double? oldestPendingAgeHours = pending.Count == 0
            ? null
            : pending.Select(o => (now - o.CreatedAt).TotalHours).Max();
        double? oldestInProgressAgeHours = inProgress.Count == 0
            ? null
            : inProgress.Select(o => (now - o.UpdatedAt).TotalHours).Max();

        var sla = new ObjectiveSlaSnapshot(
            oldestPendingAgeHours,
            oldestInProgressAgeHours,
            pending.Count,
            inProgress.Count,
            blocked.Count);

        var allProposals = proposals.List(null);
        var proposalsByStatus = Enum.GetValues<ChangeProposalStatus>()
            .ToDictionary(s => s.ToString(), s => allProposals.Count(p => p.Status == s), StringComparer.Ordinal);

        long? obsBytes = null;
        try
        {
            if (File.Exists(observationsPath))
                obsBytes = new FileInfo(observationsPath).Length;
        }
        catch
        {
            /* best-effort */
        }

        return new RuntimeStudioDiskMetrics(objectivesByStatus, sla, proposalsByStatus, obsBytes);
    }
}

public sealed record ObjectiveSlaSnapshot(
    double? OldestPendingAgeHours,
    double? OldestInProgressAgeHours,
    int PendingCount,
    int InProgressCount,
    int BlockedCount);

public sealed record RuntimeStudioDiskMetrics(
    IReadOnlyDictionary<string, int> ObjectivesByStatus,
    ObjectiveSlaSnapshot ObjectiveSla,
    IReadOnlyDictionary<string, int> ProposalsByStatus,
    long? ObservationsFileBytes);
