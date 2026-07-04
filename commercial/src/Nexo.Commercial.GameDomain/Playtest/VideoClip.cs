namespace Nexo.Commercial.GameDomain.Playtest;

/// <summary>Video clip record.</summary>
public sealed record VideoClip
{
    /// <summary>File name value.</summary>
    public string FileName { get; init; } = string.Empty;
    /// <summary>Description value.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Start seconds value.</summary>
    public double StartSeconds { get; init; }
    /// <summary>End seconds value.</summary>
    public double EndSeconds { get; init; }
    /// <summary>File size bytes value.</summary>
    public long FileSizeBytes { get; init; }
}
