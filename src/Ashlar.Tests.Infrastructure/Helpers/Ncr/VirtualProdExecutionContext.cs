using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;
using Ashlar.Infrastructure.Execution.Routing;
using Ashlar.Infrastructure.Mesh;
using Ashlar.Infrastructure.NodeCapabilityRuntime;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Profiles;

namespace Ashlar.Tests.Infrastructure.Helpers.Ncr;

/// <summary>Minimal execution context for capability-routing brick tests (provider resolved by <see cref="ProviderFactory"/>).</summary>
public sealed class VirtualProdExecutionContext : IExecutionContext
{
    public string AgentId { get; init; } = "virtual-prod-agent";
    public string BehaviorId { get; init; } = "virtual-prod-behavior";
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; } = true;

    /// <summary>Use mock-json when <c>ASHLAR_ALLOW_MOCK=1</c> for deterministic local execution.</summary>
    public string Provider { get; init; } = "mock-json";

    public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
}
