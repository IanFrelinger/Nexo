using System;

namespace Nexo.Core.Domain.Evidence;

/// <summary>Result of publishing a local artifact to an external sink.</summary>
public sealed class UploadedArtifact
{
    /// <summary>Creates an uploaded-artifact record.</summary>
    public UploadedArtifact(
        string provider,
        string externalId,
        Uri webViewUri,
        long bytes,
        string localPath,
        string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External id is required.", nameof(externalId));
        if (webViewUri is null)
            throw new ArgumentNullException(nameof(webViewUri));
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes));
        if (string.IsNullOrWhiteSpace(localPath))
            throw new ArgumentException("Local path is required.", nameof(localPath));

        Provider = provider.Trim();
        ExternalId = externalId;
        WebViewUri = webViewUri;
        Bytes = bytes;
        LocalPath = localPath;
        FileName = fileName ?? System.IO.Path.GetFileName(localPath);
    }

    /// <summary>Sink provider id (for example <c>google-drive</c>).</summary>
    public string Provider { get; }

    /// <summary>Provider-native object id.</summary>
    public string ExternalId { get; }

    /// <summary>Browser-viewable URI.</summary>
    public Uri WebViewUri { get; }

    /// <summary>Uploaded byte length.</summary>
    public long Bytes { get; }

    /// <summary>Source path on the local machine.</summary>
    public string LocalPath { get; }

    /// <summary>Remote or display file name.</summary>
    public string FileName { get; }
}
