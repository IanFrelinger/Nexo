using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Nexo.Core.Application.Evidence.Ports;
using Nexo.Core.Domain.Evidence;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using DrivePermission = Google.Apis.Drive.v3.Data.Permission;

namespace Nexo.Infrastructure.Evidence;

/// <summary>Unattended Google Drive upload via service-account credentials.</summary>
public sealed class GoogleDriveArtifactSink : IArtifactSink
{
    private readonly GoogleDriveArtifactSinkOptions _options;

    /// <summary>Creates a Google Drive sink.</summary>
    public GoogleDriveArtifactSink(GoogleDriveArtifactSinkOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Provider => "google-drive";

    /// <inheritdoc />
    public async Task<UploadedArtifact> UploadAsync(
        string localPath,
        ArtifactUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException(
                $"Artifact was not found for Drive upload: {localPath}",
                localPath);
        }

        var fileInfo = new FileInfo(localPath);
        if (fileInfo.Length == 0)
            throw new InvalidOperationException($"Artifact is empty: {localPath}");

        var folderId = request.TargetFolderId ?? _options.FolderId;
        if (string.IsNullOrWhiteSpace(folderId))
            throw new InvalidOperationException("Google Drive folder id is required.");

        var contentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? GuessContentType(localPath)
            : request.ContentType!;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? BuildRemoteFileName(localPath)
            : request.DisplayName!;
        var shareAnyone = request.ShareAnyoneWithLink || _options.ShareAnyoneWithLink;

        var credential = CredentialFactory
            .FromFile<ServiceAccountCredential>(_options.CredentialsPath)
            .ToGoogleCredential()
            .CreateScoped(DriveService.Scope.DriveFile);

        using var drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Nexo-ArtifactSink"
        });

        var metadata = new DriveFile
        {
            Name = displayName,
            Parents = new List<string> { folderId },
            MimeType = contentType
        };

        await using var stream = File.OpenRead(localPath);
        var create = drive.Files.Create(metadata, stream, contentType);
        create.Fields = "id,name,webViewLink";
        var progress = await create.UploadAsync(cancellationToken).ConfigureAwait(false);
        if (progress.Status != UploadStatus.Completed)
        {
            throw new IOException(
                progress.Exception?.Message ??
                "Google Drive upload did not complete.");
        }

        var uploaded = create.ResponseBody
                       ?? throw new InvalidOperationException(
                           "Google Drive upload completed without a file body.");

        if (shareAnyone)
        {
            await drive.Permissions.Create(
                    new DrivePermission
                    {
                        Type = "anyone",
                        Role = "reader"
                    },
                    uploaded.Id)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var webViewLink = uploaded.WebViewLink
                          ?? $"https://drive.google.com/file/d/{uploaded.Id}/view";

        return new UploadedArtifact(
            Provider,
            uploaded.Id,
            new Uri(webViewLink),
            fileInfo.Length,
            localPath,
            uploaded.Name);
    }

    /// <summary>
    /// Loads sink options from <c>NEXO_GDRIVE_CREDENTIALS</c> /
    /// <c>NEXO_GDRIVE_FOLDER_ID</c> / <c>NEXO_GDRIVE_SHARE_ANYONE</c>.
    /// Returns null when unset.
    /// </summary>
    public static GoogleDriveArtifactSinkOptions? TryLoadOptionsFromEnvironment()
    {
        var credentialsPath = Environment.GetEnvironmentVariable("NEXO_GDRIVE_CREDENTIALS");
        var folderId = Environment.GetEnvironmentVariable("NEXO_GDRIVE_FOLDER_ID");
        if (string.IsNullOrWhiteSpace(credentialsPath) ||
            string.IsNullOrWhiteSpace(folderId))
        {
            return null;
        }

        var resolvedCredentials = Path.GetFullPath(credentialsPath);
        if (!File.Exists(resolvedCredentials))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file was not found: {resolvedCredentials}",
                resolvedCredentials);
        }

        var shareAnyone = ReadBoolean(
            Environment.GetEnvironmentVariable("NEXO_GDRIVE_SHARE_ANYONE"),
            fallback: true);

        return new GoogleDriveArtifactSinkOptions(
            resolvedCredentials,
            folderId.Trim(),
            shareAnyone);
    }

    /// <summary>Creates a sink from environment variables, or null when unconfigured.</summary>
    public static GoogleDriveArtifactSink? TryCreateFromEnvironment()
    {
        var options = TryLoadOptionsFromEnvironment();
        return options is null ? null : new GoogleDriveArtifactSink(options);
    }

    private static string BuildRemoteFileName(string localPath)
    {
        var baseName = Path.GetFileNameWithoutExtension(localPath);
        var extension = Path.GetExtension(localPath);
        if (string.IsNullOrEmpty(extension))
            extension = ".bin";
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"{baseName}-{timestamp}{extension}";
    }

    private static string GuessContentType(string localPath)
        => Path.GetExtension(localPath).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".json" => "application/json",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };

    private static bool ReadBoolean(string? value, bool fallback)
        => string.IsNullOrWhiteSpace(value)
            ? fallback
            : value is "1" or "true" or "TRUE" or "yes" or "YES";
}

/// <summary>Configuration for <see cref="GoogleDriveArtifactSink"/>.</summary>
public sealed class GoogleDriveArtifactSinkOptions
{
    /// <summary>Creates options.</summary>
    public GoogleDriveArtifactSinkOptions(
        string credentialsPath,
        string folderId,
        bool shareAnyoneWithLink = true)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath))
            throw new ArgumentException("Credentials path is required.", nameof(credentialsPath));
        if (string.IsNullOrWhiteSpace(folderId))
            throw new ArgumentException("Folder id is required.", nameof(folderId));

        CredentialsPath = credentialsPath;
        FolderId = folderId;
        ShareAnyoneWithLink = shareAnyoneWithLink;
    }

    /// <summary>Service-account JSON key path.</summary>
    public string CredentialsPath { get; }

    /// <summary>Shared Drive folder id.</summary>
    public string FolderId { get; }

    /// <summary>Whether uploaded files get anyone-with-link reader ACL.</summary>
    public bool ShareAnyoneWithLink { get; }
}
