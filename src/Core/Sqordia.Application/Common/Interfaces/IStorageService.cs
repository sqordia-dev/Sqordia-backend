namespace Sqordia.Application.Common.Interfaces;

/// <summary>
/// Service interface for file storage operations
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    /// <param name="key">File key/path</param>
    /// <param name="content">File content</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the uploaded file</returns>
    Task<string> UploadFileAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a file from byte array
    /// </summary>
    Task<string> UploadFileAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    /// <param name="key">File key/path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content as stream</returns>
    Task<Stream> DownloadFileAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file as byte array
    /// </summary>
    Task<byte[]> DownloadFileBytesAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a pre-signed URL for temporary access
    /// </summary>
    /// <param name="key">File key/path</param>
    /// <param name="expirationMinutes">URL expiration time in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pre-signed URL</returns>
    Task<string> GetPresignedUrlAsync(string key, int expirationMinutes = 60, CancellationToken cancellationToken = default);
}

