using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.Infrastructure.MeshLab;

/// <summary>Snapshot of a mesh-lab task returned by the director API.</summary>
/// <param name="TaskId">Unique task identifier.</param>
/// <param name="Name">Task name, often prefixed for worker filtering.</param>
/// <param name="Status">Status string or numeric enum value.</param>
/// <param name="AssignedApiBaseUrl">Peer API base URL assigned to execute the task.</param>
/// <param name="LeaseToken">Lease token required for status PATCH operations.</param>
/// <summary>Mesh lab task snapshot.</summary>
public sealed record MeshLabTaskSnapshot(
    string TaskId,
    string? Name,
    string? Status,
    string? AssignedApiBaseUrl,
    string? LeaseToken);
