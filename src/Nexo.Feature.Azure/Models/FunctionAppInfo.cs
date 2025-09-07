using System;
using System.Collections.Generic;
using Nexo.Feature.Azure.Enums;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function app information
/// </summary>
public record FunctionAppInfo
{
    public string Name { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Runtime { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public AppServicePlanType PlanType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public int FunctionCount { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
} 