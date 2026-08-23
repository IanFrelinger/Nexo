namespace Ashlar.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// Configuration for the HTTP-based knowledge sync service.
/// </summary>
public sealed class KnowledgeSyncServiceOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "KnowledgeSync";

    /// <summary>This node's unique identifier. Default: machine name.</summary>
    public string NodeId { get; set; } = Environment.MachineName;

    /// <summary>Base URLs of peer nodes (e.g. https://ashlar-a.example.com). Used for push and pull.</summary>
    public IReadOnlyList<string> PeerUrls { get; set; } = [];
}
