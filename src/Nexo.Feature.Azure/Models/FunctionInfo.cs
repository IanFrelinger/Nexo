using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function information
/// </summary>
public record FunctionInfo
{
    public string Name { get; init; } = string.Empty;
    public string FunctionAppName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string TriggerType { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Configuration { get; init; } = new();
} 