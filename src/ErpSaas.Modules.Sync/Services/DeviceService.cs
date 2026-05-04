#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Sync.Entities;
using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Sync.Services;

public sealed class DeviceService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IDeviceService
{
    public async Task<Result<DeviceDto>> RegisterAsync(RegisterDeviceDto dto, CancellationToken ct = default)
        => await ExecuteAsync<DeviceDto>("Sync.RegisterDevice", async () =>
        {
            if (!Enum.TryParse<DeviceType>(dto.DeviceTypeCode, out var deviceType))
                return Result<DeviceDto>.Validation([Errors.Sync.InvalidDeviceType]);

            var existing = await db.Set<DeviceRegistration>()
                .FirstOrDefaultAsync(d => d.ShopId == tenant.ShopId && d.DeviceId == dto.DeviceId, ct);

            if (existing is not null)
            {
                existing.AssignedUserId = dto.AssignedUserId;
                existing.PlatformInfo   = dto.PlatformInfo;
                existing.AppVersion     = dto.AppVersion;
                existing.LastSeenAtUtc  = DateTime.UtcNow;
                existing.IsActive       = true;
                await db.SaveChangesAsync(ct);
                return Result<DeviceDto>.Success(MapDevice(existing));
            }

            var device = new DeviceRegistration
            {
                ShopId          = tenant.ShopId,
                DeviceId        = dto.DeviceId,
                BranchId        = dto.BranchId,
                AssignedUserId  = dto.AssignedUserId,
                Type            = deviceType,
                PlatformInfo    = dto.PlatformInfo,
                AppVersion      = dto.AppVersion,
                LastSeenAtUtc   = DateTime.UtcNow,
                IsActive        = true,
                CreatedAtUtc    = DateTime.UtcNow,
            };

            db.Set<DeviceRegistration>().Add(device);
            await db.SaveChangesAsync(ct);

            return Result<DeviceDto>.Success(MapDevice(device));
        }, ct);

    public async Task<Result<HeartbeatResultDto>> HeartbeatAsync(long deviceId, HeartbeatDto dto, CancellationToken ct = default)
        => await ExecuteAsync<HeartbeatResultDto>("Sync.Heartbeat", async () =>
        {
            var device = await db.Set<DeviceRegistration>()
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.ShopId == tenant.ShopId, ct);

            if (device is null) return Result<HeartbeatResultDto>.NotFound(Errors.Sync.DeviceNotFound);

            device.LastSeenAtUtc = DateTime.UtcNow;
            device.AppVersion    = dto.AppVersion;
            device.PlatformInfo  = dto.PlatformInfo;
            await db.SaveChangesAsync(ct);

            return Result<HeartbeatResultDto>.Success(new HeartbeatResultDto(
                CatalogStale: false,
                PricingStale: false,
                CustomersStale: false,
                ServerUtcNow: DateTime.UtcNow));
        }, ct);

    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken ct = default)
    {
        var devices = await db.Set<DeviceRegistration>()
            .AsNoTracking()
            .Where(d => d.ShopId == tenant.ShopId)
            .OrderByDescending(d => d.LastSeenAtUtc)
            .ToListAsync(ct);

        return devices.Select(MapDevice).ToList();
    }

    public async Task<Result<bool>> DeactivateAsync(long deviceId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Sync.DeactivateDevice", async () =>
        {
            var device = await db.Set<DeviceRegistration>()
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.ShopId == tenant.ShopId, ct);

            if (device is null) return Result<bool>.NotFound(Errors.Sync.DeviceNotFound);

            device.IsActive     = false;
            device.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct);

    private static DeviceDto MapDevice(DeviceRegistration d) => new(
        d.Id, d.DeviceId, d.BranchId, d.AssignedUserId,
        d.Type.ToString(), d.PlatformInfo, d.AppVersion,
        d.LastSeenAtUtc, d.LastSyncedAtUtc, d.IsActive);
}
