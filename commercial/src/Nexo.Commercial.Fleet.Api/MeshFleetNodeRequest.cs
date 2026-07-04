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

/// <summary>HTTP request body for registering or updating a fleet node.</summary>
public sealed record MeshFleetNodeRequest(
    string PeerId,
    string ApiBaseUrl,
    IReadOnlyDictionary<string, string>? Labels = null,
    IReadOnlyList<string>? AdvertisedBrickIds = null,
    bool Drained = false,
    int? ReportedQueueDepth = null,
    string? TrustTier = null,
    bool? Admitted = null,
    string? PeerRegistrationKey = null);
