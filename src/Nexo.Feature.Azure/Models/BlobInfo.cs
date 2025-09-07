using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Blob information
/// </summary>
public record BlobInfo
{
    public string Name { get; init; } = string.Empty;
    public long Size { get; init; }
    public DateTime LastModified { get; init; }
    public string ETag { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
    public bool IsDeleted { get; init; }
} 