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

/// <summary>HTTP request body for patching mesh task status.</summary>
public sealed record MeshTaskStatusPatchRequest(
    MeshTaskStatus Status,
    string? Reason = null,
    string? CorrelationId = null,
    string? ResultSummary = null,
    string? ResultHandle = null,
    string? LeaseToken = null);
