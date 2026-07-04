using Nexo.Core.Application.NodeCapabilityRuntime.Models;

namespace Nexo.Infrastructure.NodeCapabilityRuntime;

/// <summary>
/// Configuration options for the node capability runtime.
/// </summary>
public sealed class NodeCapabilityRuntimeOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Nexo:NodeCapabilityRuntime";

    /// <summary>Node id.</summary>
    public string NodeId { get; set; } = Environment.MachineName;

    /// <summary>Default models.</summary>
    public List<ModelDescriptor> DefaultModels { get; set; } = new();

    /// <summary>Profile refresh interval.</summary>
    public TimeSpan ProfileRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
}
