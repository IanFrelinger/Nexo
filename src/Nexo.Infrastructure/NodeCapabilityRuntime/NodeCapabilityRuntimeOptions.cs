using Nexo.Core.Application.NodeCapabilityRuntime.Models;

namespace Nexo.Infrastructure.NodeCapabilityRuntime;

/// <summary>
/// Configuration options for the node capability runtime.
/// </summary>
public sealed class NodeCapabilityRuntimeOptions
{
    public const string SectionName = "Nexo:NodeCapabilityRuntime";

    public string NodeId { get; set; } = Environment.MachineName;

    public List<ModelDescriptor> DefaultModels { get; set; } = new();

    public TimeSpan ProfileRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
}
