using LiteDB;
using Nexo.Commercial.Fleet.Contracts.Models;

namespace Nexo.Commercial.Fleet.Infrastructure;

internal static class LiteDbMeshDirectorConnection
{
    internal const string TasksCollection = "mesh_tasks";
    internal const string FleetCollection = "mesh_fleet_nodes";

    public static string ToConnectionString(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        return trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"Filename={trimmed}";
    }
}

internal sealed class MeshTaskDoc
{
    [BsonId]
    public string TaskId { get; set; } = "";

    public string? Name { get; set; }
    public int Steps { get; set; }
    public List<string> RequiredBrickIds { get; set; } = new();
    public Dictionary<string, string> Affinity { get; set; } = new();
    public int Priority { get; set; }
    public DateTimeOffset? DeadlineUtc { get; set; }
    public int Status { get; set; }
    public string? AssignedPeerId { get; set; }
    public string? AssignedApiBaseUrl { get; set; }
    public string? PlacementReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastScheduledAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? LastScheduleIdempotencyKey { get; set; }
    public string? ResultSummary { get; set; }
    public string? ResultHandle { get; set; }
    public string? LeaseToken { get; set; }
    public string? LeaseOwnerPeerId { get; set; }
    public DateTimeOffset? LeaseExpiresUtc { get; set; }
    public string? CheckpointHandle { get; set; }

    public static MeshTaskDoc FromState(MeshTaskState t) => new()
    {
        TaskId = t.TaskId,
        Name = t.Name,
        Steps = t.Steps,
        RequiredBrickIds = t.RequiredBrickIds.ToList(),
        Affinity = t.Affinity.ToDictionary(static kv => kv.Key, static kv => kv.Value),
        Priority = t.Priority,
        DeadlineUtc = t.DeadlineUtc,
        Status = (int)t.Status,
        AssignedPeerId = t.AssignedPeerId,
        AssignedApiBaseUrl = t.AssignedApiBaseUrl,
        PlacementReason = t.PlacementReason,
        AttemptCount = t.AttemptCount,
        CreatedAtUtc = t.CreatedAtUtc,
        LastScheduledAtUtc = t.LastScheduledAtUtc,
        CorrelationId = t.CorrelationId,
        IdempotencyKey = t.IdempotencyKey,
        LastScheduleIdempotencyKey = t.LastScheduleIdempotencyKey,
        ResultSummary = t.ResultSummary,
        ResultHandle = t.ResultHandle,
        LeaseToken = t.LeaseToken,
        LeaseOwnerPeerId = t.LeaseOwnerPeerId,
        LeaseExpiresUtc = t.LeaseExpiresUtc,
        CheckpointHandle = t.CheckpointHandle,
    };

    public MeshTaskState ToState() => new(
        TaskId,
        Name,
        Steps,
        RequiredBrickIds,
        Affinity,
        Priority,
        DeadlineUtc,
        (MeshTaskStatus)Status,
        AssignedPeerId,
        AssignedApiBaseUrl,
        PlacementReason,
        AttemptCount,
        CreatedAtUtc,
        LastScheduledAtUtc,
        CorrelationId,
        IdempotencyKey,
        LastScheduleIdempotencyKey,
        ResultSummary,
        ResultHandle,
        LeaseToken,
        LeaseOwnerPeerId,
        LeaseExpiresUtc,
        CheckpointHandle);
}

internal sealed class MeshFleetNodeDoc
{
    [BsonId]
    public string PeerId { get; set; } = "";

    public string ApiBaseUrl { get; set; } = "";
    public Dictionary<string, string> Labels { get; set; } = new();
    public List<string> AdvertisedBrickIds { get; set; } = new();
    public bool Drained { get; set; }
    public DateTimeOffset? LastHeartbeatUtc { get; set; }
    public DateTimeOffset RegisteredAtUtc { get; set; }
    public int ReportedQueueDepth { get; set; }
    public int TrustTier { get; set; }
    public bool Admitted { get; set; } = true;
    public string? RegistrationKeyFingerprint { get; set; }

    public static MeshFleetNodeDoc FromState(MeshFleetNodeState n) => new()
    {
        PeerId = n.PeerId,
        ApiBaseUrl = n.ApiBaseUrl,
        Labels = n.Labels.ToDictionary(static kv => kv.Key, static kv => kv.Value),
        AdvertisedBrickIds = n.AdvertisedBrickIds.ToList(),
        Drained = n.Drained,
        LastHeartbeatUtc = n.LastHeartbeatUtc,
        RegisteredAtUtc = n.RegisteredAtUtc,
        ReportedQueueDepth = n.ReportedQueueDepth,
        TrustTier = (int)n.TrustTier,
        Admitted = n.Admitted,
        RegistrationKeyFingerprint = n.RegistrationKeyFingerprint,
    };

    public MeshFleetNodeState ToState() => new(
        PeerId,
        ApiBaseUrl,
        Labels,
        AdvertisedBrickIds,
        Drained,
        LastHeartbeatUtc,
        RegisteredAtUtc,
        ReportedQueueDepth,
        (MeshFleetTrustTier)TrustTier,
        Admitted,
        RegistrationKeyFingerprint);
}
