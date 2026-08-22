using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ashlar.Brick.Contracts;
using Ashlar.Brick.Contracts.Capabilities;
using Ashlar.Contracts;
using Ashlar.Core.Application.Copilot.Models;
using Ashlar.Core.Application.Copilot.Ports;
using Ashlar.Core.Application.Product.Models;
using Ashlar.Core.Application.Product.Ports;
using Ashlar.Core.Application.Knowledge.Models;
using Ashlar.Core.Application.Knowledge.Ports;
using Ashlar.Core.Application.Agent.UseCases.RunAgent;
using Ashlar.Core.Application.Bricks;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Application.Validation.UseCases.RunValidation;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.RuntimeStudio;
using Ashlar.Infrastructure.Testing.ExecutionPlatform;
using Ashlar.Infrastructure.Execution;
using Ashlar.API.Security;
using Ashlar.Orchestration.Coordination;
using Ashlar.Orchestration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.API.Endpoints;

/// <summary>Trust dashboard snapshot with recent audit entries.</summary>
public sealed record TrustDashboardResponse(
    bool AccessBoundaryRegistered,
    bool AuditLogRegistered,
    bool IsPaused,
    IReadOnlyList<Ashlar.Core.Application.Trust.Models.DataDecisionAuditEntry> RecentAudit,
    IReadOnlyDictionary<string, int> AuditByType);
