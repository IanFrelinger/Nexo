using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function log entry
/// </summary>
public record FunctionLogEntry
{
    public string FunctionName { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string InvocationId { get; init; } = string.Empty;
    public Dictionary<string, object> Properties { get; init; } = new();
} 