using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.Mesh;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;

namespace Nexo.Tests.Infrastructure.Helpers.Ncr;

/// <summary>Minimal execution context for capability-routing brick tests (provider resolved by <see cref="ProviderFactory"/>).</summary>
public sealed class VirtualProdExecutionContext : IExecutionContext
{
    public string AgentId { get; init; } = "virtual-prod-agent";
    public string BehaviorId { get; init; } = "virtual-prod-behavior";
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; } = true;

    /// <summary>Use mock-json when <c>NEXO_ALLOW_MOCK=1</c> for deterministic local execution.</summary>
    public string Provider { get; init; } = "mock-json";

    public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
}
