using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Transport.Entities;
using ErpSaas.Modules.Transport.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Transport.Services;

public sealed class TransportService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<TransportService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), ITransportService
{
    public async Task<IReadOnlyList<TransportProviderDto>> ListProvidersAsync(CancellationToken ct = default)
        => await db.Set<TransportProvider>()
            .Where(p => !p.IsDeleted)
            .Select(p => new TransportProviderDto(p.Id, p.Name, p.ContactName, p.ContactPhone, p.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateProviderAsync(CreateTransportProviderDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Transport.CreateProvider", async () =>
        {
            var provider = new TransportProvider
            {
                ShopId = tenant.ShopId,
                Name = dto.Name,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                GstNumber = dto.GstNumber,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<TransportProvider>().Add(provider);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(provider.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ToggleProviderAsync(long id, bool isActive, CancellationToken ct = default)
        => await ExecuteAsync("Transport.ToggleProvider", async () =>
        {
            var provider = await db.Set<TransportProvider>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (provider is null) return Result<bool>.NotFound(Errors.Transport.ProviderNotFound);
            provider.IsActive = isActive;
            provider.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken ct = default)
        => await db.Set<Vehicle>()
            .Where(v => !v.IsDeleted)
            .Select(v => new VehicleDto(v.Id, v.LicensePlate, v.Model, v.MaxLoadKg, v.TransportProviderId, v.IsActive))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateVehicleAsync(CreateVehicleDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Transport.CreateVehicle", async () =>
        {
            var exists = await db.Set<Vehicle>().AnyAsync(v => v.LicensePlate == dto.LicensePlate, ct);
            if (exists) return Result<long>.Conflict(Errors.Transport.LicensePlateExists);

            var vehicle = new Vehicle
            {
                ShopId = tenant.ShopId,
                LicensePlate = dto.LicensePlate,
                Model = dto.Model,
                MaxLoadKg = dto.MaxLoadKg,
                TransportProviderId = dto.TransportProviderId,
                DriverName = dto.DriverName,
                DriverPhone = dto.DriverPhone,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Vehicle>().Add(vehicle);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(vehicle.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ToggleVehicleAsync(long id, bool isActive, CancellationToken ct = default)
        => await ExecuteAsync("Transport.ToggleVehicle", async () =>
        {
            var vehicle = await db.Set<Vehicle>().FirstOrDefaultAsync(v => v.Id == id, ct);
            if (vehicle is null) return Result<bool>.NotFound(Errors.Transport.VehicleNotFound);
            vehicle.IsActive = isActive;
            vehicle.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<IReadOnlyList<DeliveryDto>> ListDeliveriesAsync(CancellationToken ct = default)
        => await db.Set<Delivery>()
            .Where(d => !d.IsDeleted)
            .Select(d => new DeliveryDto(
                d.Id, d.DeliveryNumber, d.ReferenceType, d.ReferenceId,
                d.ReferenceNumberSnapshot, d.CustomerId, d.CustomerNameSnapshot,
                d.VehicleId, d.TransportProviderId, d.Status,
                d.ScheduledDate, d.DeliveredDate, d.DeliveryAddress, d.Notes))
            .ToListAsync(ct);

    public async Task<Result<long>> CreateDeliveryAsync(CreateDeliveryDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Transport.CreateDelivery", async () =>
        {
            var number = await sequence.NextAsync(Constants.SequenceCodes.Delivery, tenant.ShopId, ct);
            var delivery = new Delivery
            {
                ShopId = tenant.ShopId,
                DeliveryNumber = number,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                ReferenceNumberSnapshot = dto.ReferenceNumberSnapshot,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = dto.CustomerNameSnapshot,
                VehicleId = dto.VehicleId,
                TransportProviderId = dto.TransportProviderId,
                Status = DeliveryStatus.Scheduled,
                ScheduledDate = dto.ScheduledDate,
                DeliveryAddress = dto.DeliveryAddress,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Delivery>().Add(delivery);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(delivery.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateDeliveryStatusAsync(long id, UpdateDeliveryStatusDto dto, CancellationToken ct = default)
        => await ExecuteAsync("Transport.UpdateDeliveryStatus", async () =>
        {
            var delivery = await db.Set<Delivery>().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (delivery is null) return Result<bool>.NotFound(Errors.Transport.DeliveryNotFound);
            if (delivery.Status == DeliveryStatus.Delivered)
                return Result<bool>.Conflict(Errors.Transport.DeliveryAlreadyDelivered);

            var oldStatus = delivery.Status;
            delivery.Status = dto.Status;
            if (dto.Status == DeliveryStatus.Delivered)
                delivery.DeliveredDate = DateTime.UtcNow;
            delivery.UpdatedAtUtc = DateTime.UtcNow;

            db.Set<DeliveryLog>().Add(new DeliveryLog
            {
                DeliveryId = id,
                Status = dto.Status,
                Notes = dto.Notes,
                LoggedByUserId = tenant.CurrentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
}
