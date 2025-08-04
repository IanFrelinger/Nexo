using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Azure.Interfaces;

/// <summary>
/// Provides Azure Blob Storage operations and container management
/// </summary>
public interface IBlobStorageAdapter
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="filePath">Path to the file to upload</param>
    /// <param name="metadata">Optional metadata for the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing upload information</returns>
    Task<BlobUploadInfo> UploadFileAsync(string containerName, string blobName, string filePath, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads data from a stream to Azure Blob Storage
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="stream">Stream containing the data to upload</param>
    /// <param name="metadata">Optional metadata for the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing upload information</returns>
    Task<BlobUploadInfo> UploadStreamAsync(string containerName, string blobName, Stream stream, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob to a local file
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="filePath">Local file path to save the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing download information</returns>
    Task<BlobDownloadInfo> DownloadFileAsync(string containerName, string blobName, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob to a stream
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="stream">Stream to write the blob data to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing download information</returns>
    Task<BlobDownloadInfo> DownloadStreamAsync(string containerName, string blobName, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs in a container with optional filtering
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="prefix">Optional prefix to filter blobs</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of blob information</returns>
    Task<List<BlobInfo>> ListBlobsAsync(string containerName, string? prefix = null, int? maxResults = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets blob properties and metadata
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing blob properties</returns>
    Task<BlobProperties> GetBlobPropertiesAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets blob metadata
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="metadata">Metadata to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success</returns>
    Task<AzureOperationResult> SetBlobMetadataAsync(string containerName, string blobName, Dictionary<string, string> metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating deletion success</returns>
    Task<AzureOperationResult> DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed URL for blob access
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="expiresIn">Duration for which the URL is valid</param>
    /// <param name="permissions">Access permissions (read, write, delete)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the pre-signed URL</returns>
    Task<string> GenerateSasUrlAsync(string containerName, string blobName, TimeSpan expiresIn, BlobSasPermissions permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new blob container
    /// </summary>
    /// <param name="containerName">Name of the container to create</param>
    /// <param name="publicAccess">Public access level for the container</param>
    /// <param name="metadata">Optional metadata for the container</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating creation success</returns>
    Task<AzureOperationResult> CreateContainerAsync(string containerName, BlobContainerPublicAccessType publicAccess = BlobContainerPublicAccessType.None, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob container
    /// </summary>
    /// <param name="containerName">Name of the container to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating deletion success</returns>
    Task<AzureOperationResult> DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all blob containers
    /// </summary>
    /// <param name="prefix">Optional prefix to filter containers</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of container information</returns>
    Task<List<ContainerInfo>> ListContainersAsync(string? prefix = null, int? maxResults = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists
    /// </summary>
    /// <param name="containerName">Name of the blob container</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating blob existence</returns>
    Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a blob from one location to another
    /// </summary>
    /// <param name="sourceContainerName">Source container name</param>
    /// <param name="sourceBlobName">Source blob name</param>
    /// <param name="destinationContainerName">Destination container name</param>
    /// <param name="destinationBlobName">Destination blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating copy success</returns>
    Task<AzureOperationResult> CopyBlobAsync(string sourceContainerName, string sourceBlobName, string destinationContainerName, string destinationBlobName, CancellationToken cancellationToken = default);
}

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

/// <summary>
/// Blob download information
/// </summary>
public record BlobDownloadInfo
{
    public string ContainerName { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ETag { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
}

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

/// <summary>
/// Blob SAS permissions
/// </summary>
[Flags]
public enum BlobSasPermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    List = 8,
    Add = 16,
    Create = 32,
    Update = 64,
    Process = 128,
    All = Read | Write | Delete | List | Add | Create | Update | Process
}

/// <summary>
/// Blob container public access types
/// </summary>
public enum BlobContainerPublicAccessType
{
    None,
    Blob,
    Container
} 