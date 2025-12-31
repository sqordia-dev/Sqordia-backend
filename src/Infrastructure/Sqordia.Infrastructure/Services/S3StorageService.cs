using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sqordia.Application.Common.Interfaces;
using System.Text;

namespace Sqordia.Infrastructure.Services;

/// <summary>
/// AWS S3 storage service configuration
/// </summary>
public class AwsStorageSettings
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "ca-central-1";
}

/// <summary>
/// AWS S3 implementation of storage service
/// </summary>
public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsStorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(
        IAmazonS3 s3Client,
        IOptions<AwsStorageSettings> settings,
        ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            var url = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{key}";
            _logger.LogInformation("File uploaded to S3: {Key}", key);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {Key}", key);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        return await UploadFileAsync(key, stream, contentType, cancellationToken);
    }

    public async Task<Stream> DownloadFileAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found in S3: {Key}", key);
            throw new FileNotFoundException($"File '{key}' not found in storage", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from S3: {Key}", key);
            throw;
        }
    }

    public async Task<byte[]> DownloadFileBytesAsync(string key, CancellationToken cancellationToken = default)
    {
        using var stream = await DownloadFileAsync(key, cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    public async Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            _logger.LogInformation("File deleted from S3: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {Key}", key);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in S3: {Key}", key);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string key, int expirationMinutes = 60, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            _logger.LogInformation("Generated pre-signed URL for {Key}, expires in {Minutes} minutes", key, expirationMinutes);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed URL for S3: {Key}", key);
            throw;
        }
    }
}

