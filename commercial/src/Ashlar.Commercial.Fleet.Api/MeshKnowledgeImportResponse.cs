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

/// <summary>HTTP response payload for a knowledge import operation.</summary>
public sealed record MeshKnowledgeImportResponse(
    int AdaptationsApplied,
    int AdaptationsSkipped,
    int PatternsApplied,
    int PatternsSkipped);
