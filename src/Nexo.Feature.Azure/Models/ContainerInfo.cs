using System;
using System.Collections.Generic;
using Nexo.Feature.Azure.Enums;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Container information
/// </summary>
public record ContainerInfo
{
    public string Name { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public string ETag { get; init; } = string.Empty;
    public BlobContainerPublicAccessType PublicAccess { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public bool HasImmutabilityPolicy { get; init; }
    public bool HasLegalHold { get; init; }
} 