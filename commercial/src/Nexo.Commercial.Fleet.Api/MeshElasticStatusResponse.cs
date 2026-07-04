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

/// <summary>HTTP response payload describing elastic scheduling status.</summary>
public sealed record MeshElasticStatusResponse(
    IReadOnlyDictionary<string, int> TaskCountsByStatus,
    IReadOnlyList<MeshElasticWorkerSnapshot> Workers,
    DateTimeOffset GeneratedAtUtc);
