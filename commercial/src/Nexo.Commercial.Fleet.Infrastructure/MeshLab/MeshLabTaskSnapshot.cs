using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Models;

namespace Nexo.Commercial.Fleet.Infrastructure.MeshLab;

/// <summary>Mesh lab task snapshot operation.</summary>
public sealed record MeshLabTaskSnapshot(
    string TaskId,
    string? Name,
    string? Status,
    string? AssignedApiBaseUrl,
    string? LeaseToken);
