using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Nexo.API.Security;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;
using Nexo.Commercial.Fleet.Infrastructure;

namespace Nexo.Commercial.Fleet.Api;

/// <summary>HTTP response payload for a mesh task.</summary>
public sealed record MeshTaskResponse(
    string TaskId,
    string? Name,
    int Steps,
    IReadOnlyList<string> RequiredBrickIds,
    IReadOnlyDictionary<string, string> Affinity,
    int Priority,
    DateTimeOffset? DeadlineUtc,
    string Status,
    string? AssignedPeerId,
    string? AssignedApiBaseUrl,
    string? PlacementReason,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastScheduledAtUtc,
    string? CorrelationId,
    string? IdempotencyKey,
    string? LastScheduleIdempotencyKey,
    string? ResultSummary,
    string? ResultHandle,
    string? LeaseToken,
    string? LeaseOwnerPeerId,
    DateTimeOffset? LeaseExpiresUtc,
    string? CheckpointHandle);
