namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Optional background pull of knowledge JSON from peer Nexo.API hosts (Phase 4).
/// Binds from <see cref="SectionPath"/>.
/// </summary>
public sealed class MeshPeerKnowledgeSyncOptions
{
    public const string SectionPath = "Nexo:Mesh:KnowledgeSync";

    /// <summary>When true, registers the background pull loop.</summary>
    public bool Enabled { get; set; }

    /// <summary>Absolute base URLs of peer APIs (e.g. https://worker:8080/).</summary>
    public string[] PeerBaseUrls { get; set; } = [];

    /// <summary>Minutes between pull rounds.</summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>Overlap window multiplier for the since query (default 2 × interval).</summary>
    public int SinceLookbackMultiplier { get; set; } = 2;

    /// <summary>Max adaptation records per export request.</summary>
    public int MaxAdaptations { get; set; } = 500;

    /// <summary>Max patterns per export request.</summary>
    public int MaxPatterns { get; set; } = 500;
}
