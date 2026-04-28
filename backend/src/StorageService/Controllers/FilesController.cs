using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using StorageService.Models;
using StorageService.Repositories;
using StorageService.Services;
using System.Security.Claims;
using Amazon.S3.Model;

namespace StorageService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IStorageRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly JwtHelper _jwtHelper;
    private readonly Amazon.S3.IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public FilesController(IStorageRepository repository, IFileStorageService fileStorage, JwtHelper jwtHelper, Amazon.S3.IAmazonS3 s3Client, IConfiguration config)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _jwtHelper = jwtHelper;
        _s3Client = s3Client;
        _bucketName = config["MINIO_BUCKET_NAME"] ?? "storage";
    }

    [HttpGet]
    public async Task<ActionResult<List<FileMetadata>>> GetFiles(
        [FromQuery] string? category = null, 
        [FromQuery] string? type = null, 
        [FromQuery] string? search = null, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (!companyId.HasValue) return Unauthorized();

        var files = await _repository.GetFilesAsync(companyId.Value, userId, category, type, search, skip, take);
        return Ok(files);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB
    public async Task<ActionResult<FileMetadata>> UploadFile([FromForm] IFormFile file, [FromForm] bool isPrivate = false)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (!companyId.HasValue || userId == 0) return Unauthorized();

        try
        {
            var relativePath = await _fileStorage.SaveFileAsync(file);
            
            var metadata = new FileMetadata
            {
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                Path = _fileStorage.GetFileUrl(relativePath),
                OwnerId = userId,
                CompanyId = companyId.Value,
                IsPrivate = isPrivate,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateAsync(metadata);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}/important")]
    public async Task<IActionResult> ToggleImportant(string id, [FromBody] bool isImportant)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Worker";

        if (!companyId.HasValue) return Unauthorized();
        if (role != "Boss") return Forbid("Only Boss can mark files as important");

        var metadata = await _repository.GetByIdAsync(id);
        if (metadata == null || metadata.CompanyId != companyId.Value) return NotFound();

        metadata.IsImportant = isImportant;
        await _repository.UpdateAsync(metadata);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (!companyId.HasValue) return Unauthorized();

        var metadata = await _repository.GetByIdAsync(id);
        if (metadata == null || metadata.CompanyId != companyId.Value) return NotFound();

        // Only owner or Boss can delete
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Worker";
        try
        {
            if (metadata.OwnerId != userId && role != "Boss") return Forbid();

            // Extract relative path from URL (format: /storage/uploads/20260203/guid.ext)
            var relativePath = metadata.Path.Split("/uploads/").Last();
            await _fileStorage.DeleteFileAsync(relativePath);
            await _repository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("/uploads/{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFile(string path)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, path);
            Response.Headers.Append("Cache-Control", "public,max-age=31536000");
            return File(response.ResponseStream, response.Headers.ContentType);
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
