using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Alert information
/// </summary>
public record Alert
{
    public string AlertId { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime RaisedAt { get; init; }
    public bool IsAcknowledged { get; init; }
} 