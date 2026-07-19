using System;

namespace Nexo.Core.Domain.Evidence;

/// <summary>Named host-state sample taken during a session.</summary>
public sealed class EvidenceCheckpoint
{
    /// <summary>Creates a checkpoint.</summary>
    public EvidenceCheckpoint(string label, string? statusJson = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Checkpoint label is required.", nameof(label));

        Label = label.Trim();
        StatusJson = statusJson;
    }

    /// <summary>Stable checkpoint name (for example <c>initial</c>).</summary>
    public string Label { get; }

    /// <summary>Optional JSON snapshot of host status at this checkpoint.</summary>
    public string? StatusJson { get; }
}
