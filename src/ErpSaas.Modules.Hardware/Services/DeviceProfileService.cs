using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hardware.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hardware.Services;

public sealed class DeviceProfileService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IDeviceProfileService
{
    public async Task<Result<long>> CreateAsync(CreateDeviceProfileDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.DeviceProfile.Create", async () =>
        {
            var exists = await _db.Set<DeviceProfile>()
                .AnyAsync(d => d.DeviceId == dto.DeviceId, ct);
            if (exists)
                return Result<long>.Conflict(Errors.Hardware.DeviceAlreadyRegistered);

            var entity = new DeviceProfile
            {
                ShopId = tenant.ShopId,
                DeviceId = dto.DeviceId,
                Class = dto.Class,
                VendorCode = dto.VendorCode,
                ModelCode = dto.ModelCode,
                ConnectionJson = dto.ConnectionJson,
                Role = dto.Role,
                IsDefault = dto.IsDefault,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<DeviceProfile>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> UpdateAsync(long id, UpdateDeviceProfileDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.DeviceProfile.Update", async () =>
        {
            var entity = await _db.Set<DeviceProfile>().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hardware.DeviceNotFound);

            if (dto.VendorCode is not null) entity.VendorCode = dto.VendorCode;
            if (dto.ModelCode is not null) entity.ModelCode = dto.ModelCode;
            if (dto.ConnectionJson is not null) entity.ConnectionJson = dto.ConnectionJson;
            if (dto.Role is not null) entity.Role = dto.Role;
            if (dto.IsDefault.HasValue) entity.IsDefault = dto.IsDefault.Value;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.DeviceProfile.Delete", async () =>
        {
            var entity = await _db.Set<DeviceProfile>().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hardware.DeviceNotFound);

            _db.Set<DeviceProfile>().Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<DeviceProfileDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.Set<DeviceProfile>().FirstOrDefaultAsync(d => d.Id == id, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<DeviceProfileDto>> ListAsync(CancellationToken ct = default)
        => await _db.Set<DeviceProfile>()
            .OrderBy(d => d.Role).ThenBy(d => d.DeviceId)
            .Select(d => Map(d))
            .ToListAsync(ct);

    private static DeviceProfileDto Map(DeviceProfile d) => new(
        d.Id, d.DeviceId, d.Class, d.VendorCode, d.ModelCode,
        d.ConnectionJson, d.Role, d.IsDefault, d.IsActive, d.LastUsedAtUtc);
}
