namespace Nexo.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// Configuration for the network agent directory (peer URLs for discovery).
/// </summary>
public sealed class NetworkAgentDirectoryOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "NetworkAgentDirectory";

    /// <summary>This node's unique identifier. Default: machine name.</summary>
    public string NodeId { get; set; } = Environment.MachineName;

    /// <summary>Base URLs of peer nodes for agent discovery.</summary>
    public IReadOnlyList<string> PeerUrls { get; set; } = [];

    /// <summary>Max age of discovered entries in seconds before refresh. Default 60.</summary>
    public int DiscoveredEntryMaxAgeSeconds { get; set; } = 60;
}
