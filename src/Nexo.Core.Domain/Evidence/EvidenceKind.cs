namespace Nexo.Core.Domain.Evidence;

/// <summary>Kind of evidence artifact captured during a host/agent session.</summary>
public enum EvidenceKind
{
    /// <summary>Still image capture.</summary>
    Screenshot = 0,

    /// <summary>Video capture (for example MP4).</summary>
    Video = 1,

    /// <summary>Structured or markdown report.</summary>
    Report = 2,

    /// <summary>Text or structured log.</summary>
    Log = 3,

    /// <summary>Other binary or text artifact.</summary>
    Other = 4
}
