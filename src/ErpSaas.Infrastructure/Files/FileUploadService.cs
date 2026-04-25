#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Files;
using ErpSaas.Infrastructure.Metering;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Files;

public sealed class FileUploadService(
    TenantDbContext db,
    PlatformDbContext platform,
    IFileStorage storage,
    ITenantContext tenantContext,
    IErrorLogger errorLogger,
    IUsageMeterService? usageMeter = null)
    : BaseService<TenantDbContext>(db, errorLogger), IFileUploadService
{
    public async Task<Result<UploadFileResponse>> UploadAsync(
        UploadFileRequest request, CancellationToken ct = default)
        => await ExecuteAsync<UploadFileResponse>("Files.Upload", async () =>
        {
            var config = await platform.FileUploadConfigs
                .FirstOrDefaultAsync(c => c.Purpose == request.Purpose && c.IsActive, ct);

            if (config is not null)
            {
                var ext = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
                var allowed = config.AllowedExtensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!allowed.Contains(ext))
                    return Result<UploadFileResponse>.Conflict(Errors.Files.ExtensionConflict(ext));

                if (request.SizeBytes > config.MaxSizeBytes)
                    return Result<UploadFileResponse>.Conflict(Errors.Files.SizeConflict(config.MaxSizeBytes));
            }

            if (usageMeter is not null)
            {
                var sizeKb = Math.Max(1, request.SizeBytes / 1024);
                var quota = await usageMeter.CheckQuotaAsync(MeterCodes.StorageMb, sizeKb / 1024 + 1, ct);
                if (quota.IsDenied)
                    return Result<UploadFileResponse>.Conflict(Errors.Files.StorageQuotaExceeded);
            }

            var key = await storage.SaveAsync(request.Content, request.OriginalFileName, request.ContentType, ct);

            var entity = new UploadedFile
            {
                ShopId = tenantContext.ShopId,
                OriginalFileName = request.OriginalFileName,
                StorageKey = key,
                ContentType = request.ContentType,
                SizeBytes = request.SizeBytes,
                Purpose = request.Purpose,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                UploadedAtUtc = DateTime.UtcNow,
                UploadedByUserId = tenantContext.CurrentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            db.UploadedFiles.Add(entity);
            await db.SaveChangesAsync(ct);

            if (usageMeter is not null)
            {
                var mbDelta = Math.Max(1L, request.SizeBytes / (1024 * 1024));
                await usageMeter.IncrementAsync(MeterCodes.StorageMb, mbDelta, "UploadedFile", entity.Id, ct: ct);
            }

            return Result<UploadFileResponse>.Success(ToDto(entity, storage.GetPublicUrl(key)));
        }, ct, useTransaction: true);

    public async Task<UploadFileResponse?> GetAsync(long id, CancellationToken ct = default)
    {
        var f = await db.UploadedFiles
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return f is null ? null : ToDto(f, storage.GetPublicUrl(f.StorageKey));
    }

    public async Task<IReadOnlyList<UploadFileResponse>> ListByEntityAsync(
        string entityType, long entityId, CancellationToken ct = default)
    {
        var files = await db.UploadedFiles
            .Where(x => x.EntityType == entityType && x.EntityId == entityId && !x.IsDeleted)
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToListAsync(ct);
        return files.Select(f => ToDto(f, storage.GetPublicUrl(f.StorageKey))).ToList();
    }

    public async Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Files.Delete", async () =>
        {
            var file = await db.UploadedFiles
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (file is null)
                return Result<bool>.NotFound(Errors.Files.NotFound);

            await storage.DeleteAsync(file.StorageKey, ct);

            file.IsDeleted = true;
            file.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    private static UploadFileResponse ToDto(UploadedFile f, string url) =>
        new(f.Id, f.OriginalFileName, f.ContentType, f.SizeBytes,
            f.Purpose, f.EntityType, f.EntityId, url, f.UploadedAtUtc);
}
