using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StorageService.Services;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(IAmazonS3 s3Client, IConfiguration config, ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = config["MINIO_BUCKET_NAME"] ?? "storage";
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var subDir = DateTime.UtcNow.ToString("yyyyMMdd");
        var key = $"{subDir}/{fileName}";

        try
        {
            using (var stream = file.OpenReadStream())
            {
                var uploadRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType
                };
                await _s3Client.PutObjectAsync(uploadRequest);
            }

            _logger.LogInformation("File {FileName} saved to S3 bucket {BucketName} with key {Key}", file.FileName, _bucketName, key);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file to S3");
            throw;
        }
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = relativePath
            } ;
            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("File {Key} deleted from S3 bucket {BucketName}", relativePath, _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3");
            throw;
        }
    }

    public string GetFileUrl(string relativePath)
    {
        // This URL will be proxied through the Gateway and StorageService
        return $"/storage/uploads/{relativePath}";
    }
}
