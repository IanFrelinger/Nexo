using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Blob properties
/// </summary>
public record BlobProperties
{
    public string ETag { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public long ContentLength { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string ContentEncoding { get; init; } = string.Empty;
    public string ContentLanguage { get; init; } = string.Empty;
    public string ContentMD5 { get; init; } = string.Empty;
    public string CacheControl { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
    public Dictionary<string, string> Tags { get; init; } = new();
} 