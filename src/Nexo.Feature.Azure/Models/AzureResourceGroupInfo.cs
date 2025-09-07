using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Azure resource group information
/// </summary>
public record AzureResourceGroupInfo
{
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ProvisioningState { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public int ResourceCount { get; init; }
} 