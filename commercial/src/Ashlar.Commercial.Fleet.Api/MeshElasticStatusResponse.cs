using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Ashlar.API.Security;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;

namespace Ashlar.Commercial.Fleet.Api;

/// <summary>HTTP response payload describing elastic scheduling status.</summary>
public sealed record MeshElasticStatusResponse(
    IReadOnlyDictionary<string, int> TaskCountsByStatus,
    IReadOnlyList<MeshElasticWorkerSnapshot> Workers,
    DateTimeOffset GeneratedAtUtc);
