using Nexo.Core.Application.Evidence.Ports;
using Nexo.Core.Domain.Evidence;

namespace Nexo.Infrastructure.Evidence;

/// <summary>
/// Test/dev sink that copies artifacts into a local directory instead of a cloud provider.
/// </summary>
public sealed class LocalCopyArtifactSink : IArtifactSink
{
    private readonly string _destinationRoot;

    /// <summary>Creates a local-copy sink.</summary>
    public LocalCopyArtifactSink(string destinationRoot)
    {
        if (string.IsNullOrWhiteSpace(destinationRoot))
            throw new ArgumentException("Destination root is required.", nameof(destinationRoot));

        _destinationRoot = Path.GetFullPath(destinationRoot);
        Directory.CreateDirectory(_destinationRoot);
    }

    /// <inheritdoc />
    public string Provider => "local-copy";

    /// <inheritdoc />
    public Task<UploadedArtifact> UploadAsync(
        string localPath,
        ArtifactUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(localPath))
            throw new FileNotFoundException("Artifact was not found.", localPath);

        var info = new FileInfo(localPath);
        if (info.Length == 0)
            throw new InvalidOperationException($"Artifact is empty: {localPath}");

        var fileName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? Path.GetFileName(localPath)
            : request.DisplayName!;
        var destination = Path.Combine(_destinationRoot, fileName);
        File.Copy(localPath, destination, overwrite: true);

        var uri = new Uri(destination);
        return Task.FromResult(new UploadedArtifact(
            Provider,
            externalId: destination,
            webViewUri: uri,
            bytes: info.Length,
            localPath: localPath,
            fileName: fileName));
    }
}
