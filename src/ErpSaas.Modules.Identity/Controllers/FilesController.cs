using ErpSaas.Infrastructure.Files;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/files")]
[Authorize]
public sealed class FilesController(IFileUploadService fileUploadService) : BaseController
{
    [HttpPost("upload")]
    [RequirePermission("Files.Upload")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB hard cap at transport layer
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string purpose,
        [FromForm] string? entityType = null,
        [FromForm] long? entityId = null,
        CancellationToken ct = default)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        await using var stream = file.OpenReadStream();
        var request = new UploadFileRequest(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            purpose,
            entityType,
            entityId);

        var result = await fileUploadService.UploadAsync(request, ct);
        return result.IsSuccess ? Ok(result.Value) : Conflict(result.Errors);
    }

    [HttpGet("{id:long}")]
    [RequirePermission("Files.View")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var file = await fileUploadService.GetAsync(id, ct);
        return file is null ? NotFound() : Ok(file);
    }

    [HttpGet("entity/{entityType}/{entityId:long}")]
    [RequirePermission("Files.View")]
    public async Task<IActionResult> ListByEntity(
        string entityType, long entityId, CancellationToken ct)
        => Ok(await fileUploadService.ListByEntityAsync(entityType, entityId, ct));

    [HttpDelete("{id:long}")]
    [RequirePermission("Files.Delete")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await fileUploadService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok()
            : result.Errors.Any(e => e.Contains("FILE_001")) ? NotFound()
            : Conflict(result.Errors);
    }
}
