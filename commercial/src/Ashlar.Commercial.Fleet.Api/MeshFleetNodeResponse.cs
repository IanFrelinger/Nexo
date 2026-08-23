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

/// <summary>HTTP response payload for a registered fleet node.</summary>
public sealed record MeshFleetNodeResponse(
    string PeerId,
    string ApiBaseUrl,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<string> AdvertisedBrickIds,
    bool Drained,
    DateTimeOffset? LastHeartbeatUtc,
    DateTimeOffset RegisteredAtUtc,
    int ReportedQueueDepth,
    string TrustTier,
    bool Admitted,
    string? RegistrationKeyFingerprint);
