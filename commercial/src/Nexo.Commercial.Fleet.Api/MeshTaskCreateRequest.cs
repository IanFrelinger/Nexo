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

/// <summary>HTTP request body for creating a mesh task.</summary>
public sealed record MeshTaskCreateRequest(
    string? Name,
    int Steps,
    IReadOnlyList<string>? RequiredBrickIds,
    IReadOnlyDictionary<string, string>? Affinity,
    int Priority = 0,
    DateTimeOffset? DeadlineUtc = null,
    string? CorrelationId = null,
    string? IdempotencyKey = null);
