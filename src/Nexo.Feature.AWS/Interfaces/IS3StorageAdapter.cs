using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// S3 storage adapter interface for file operations
    /// </summary>
    public interface IS3StorageAdapter
    {
        /// <summary>
        /// Uploads a file to S3
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="filePath">Local file path</param>
        /// <param name="metadata">Optional metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result</returns>
        Task<S3UploadResult> UploadFileAsync(string bucketName, string key, string filePath, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a stream to S3
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="stream">Data stream</param>
        /// <param name="contentType">Content type</param>
        /// <param name="metadata">Optional metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result</returns>
        Task<S3UploadResult> UploadStreamAsync(string bucketName, string key, Stream stream, string contentType, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file from S3
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="localFilePath">Local file path to save to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Download result</returns>
        Task<S3DownloadResult> DownloadFileAsync(string bucketName, string key, string localFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file from S3 as a stream
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Download result with stream</returns>
        Task<S3DownloadResult> DownloadStreamAsync(string bucketName, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists objects in an S3 bucket
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="prefix">Optional prefix filter</param>
        /// <param name="maxKeys">Maximum number of keys to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List result</returns>
        Task<S3ListResult> ListObjectsAsync(string bucketName, string? prefix = null, int maxKeys = 1000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an object from S3
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delete result</returns>
        Task<S3DeleteResult> DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets object metadata from S3
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object metadata</returns>
        Task<S3ObjectMetadata> GetObjectMetadataAsync(string bucketName, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a pre-signed URL for S3 operations
        /// </summary>
        /// <param name="bucketName">S3 bucket name</param>
        /// <param name="key">S3 object key</param>
        /// <param name="expirationMinutes">URL expiration time in minutes</param>
        /// <param name="operation">S3 operation (GET, PUT, DELETE)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Pre-signed URL</returns>
        Task<string> GeneratePresignedUrlAsync(string bucketName, string key, int expirationMinutes, string operation = "GET", CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an S3 bucket
        /// </summary>
        /// <param name="bucketName">Bucket name</param>
        /// <param name="region">AWS region</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bucket creation result</returns>
        Task<S3BucketResult> CreateBucketAsync(string bucketName, string region, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an S3 bucket
        /// </summary>
        /// <param name="bucketName">Bucket name</param>
        /// <param name="forceDelete">Force delete even if bucket contains objects</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bucket deletion result</returns>
        Task<S3BucketResult> DeleteBucketAsync(string bucketName, bool forceDelete = false, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// S3 upload result
    /// </summary>
    public class S3UploadResult
    {
        /// <summary>
        /// Whether the upload was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Upload message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// S3 object URL
        /// </summary>
        public string? ObjectUrl { get; set; }

        /// <summary>
        /// S3 object ETag
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Upload timestamp
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Error details if upload failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// S3 download result
    /// </summary>
    public class S3DownloadResult
    {
        /// <summary>
        /// Whether the download was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Download message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Downloaded file stream (for stream downloads)
        /// </summary>
        public Stream? Stream { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Download timestamp
        /// </summary>
        public DateTime DownloadedAt { get; set; }

        /// <summary>
        /// Error details if download failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// S3 list result
    /// </summary>
    public class S3ListResult
    {
        /// <summary>
        /// Whether the list operation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// List message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// List of S3 objects
        /// </summary>
        public List<S3ObjectInfo> Objects { get; set; } = new List<S3ObjectInfo>();

        /// <summary>
        /// Common prefixes (for folder-like structure)
        /// </summary>
        public List<string> CommonPrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Whether there are more objects to list
        /// </summary>
        public bool IsTruncated { get; set; }

        /// <summary>
        /// Continuation token for pagination
        /// </summary>
        public string? NextContinuationToken { get; set; }

        /// <summary>
        /// List timestamp
        /// </summary>
        public DateTime ListedAt { get; set; }

        /// <summary>
        /// Error details if list failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// S3 object information
    /// </summary>
    public class S3ObjectInfo
    {
        /// <summary>
        /// Object key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Object size in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Object ETag
        /// </summary>
        public string ETag { get; set; } = string.Empty;

        /// <summary>
        /// Storage class
        /// </summary>
        public string StorageClass { get; set; } = string.Empty;

        /// <summary>
        /// Object owner
        /// </summary>
        public string? Owner { get; set; }
    }

    /// <summary>
    /// S3 delete result
    /// </summary>
    public class S3DeleteResult
    {
        /// <summary>
        /// Whether the delete was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Delete message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Delete timestamp
        /// </summary>
        public DateTime DeletedAt { get; set; }

        /// <summary>
        /// Error details if delete failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// S3 object metadata
    /// </summary>
    public class S3ObjectMetadata
    {
        /// <summary>
        /// Object key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Object size in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Object ETag
        /// </summary>
        public string ETag { get; set; } = string.Empty;

        /// <summary>
        /// Storage class
        /// </summary>
        public string StorageClass { get; set; } = string.Empty;

        /// <summary>
        /// Custom metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Object version ID
        /// </summary>
        public string? VersionId { get; set; }
    }

    /// <summary>
    /// S3 bucket operation result
    /// </summary>
    public class S3BucketResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Bucket name
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Operation timestamp
        /// </summary>
        public DateTime OperatedAt { get; set; }

        /// <summary>
        /// Error details if operation failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }
} 