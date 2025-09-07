using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Blob upload information
/// </summary>
public record BlobUploadInfo
{
    public string ContainerName { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ETag { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
} 