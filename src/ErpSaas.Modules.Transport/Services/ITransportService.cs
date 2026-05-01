using ErpSaas.Modules.Transport.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Transport.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record TransportProviderDto(long Id, string Name, string? ContactName, string? ContactPhone, bool IsActive);
public record CreateTransportProviderDto(string Name, string? ContactName, string? ContactPhone, string? GstNumber);

public record VehicleDto(long Id, string LicensePlate, string Model, decimal MaxLoadKg, long? TransportProviderId, bool IsActive);
public record CreateVehicleDto(string LicensePlate, string Model, decimal MaxLoadKg, long? TransportProviderId, string? DriverName, string? DriverPhone);

public record DeliveryDto(
    long Id, string DeliveryNumber, DeliveryReferenceType ReferenceType, long ReferenceId,
    string ReferenceNumberSnapshot, long CustomerId, string CustomerNameSnapshot,
    long? VehicleId, long? TransportProviderId, DeliveryStatus Status,
    DateTime ScheduledDate, DateTime? DeliveredDate, string DeliveryAddress, string? Notes);

public record CreateDeliveryDto(
    DeliveryReferenceType ReferenceType, long ReferenceId, string ReferenceNumberSnapshot,
    long CustomerId, string CustomerNameSnapshot,
    long? VehicleId, long? TransportProviderId,
    DateTime ScheduledDate, string DeliveryAddress, string? Notes);

public record UpdateDeliveryStatusDto(DeliveryStatus Status, string? Notes);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ITransportService
{
    Task<IReadOnlyList<TransportProviderDto>> ListProvidersAsync(CancellationToken ct = default);
    Task<Result<long>> CreateProviderAsync(CreateTransportProviderDto dto, CancellationToken ct = default);
    Task<Result<bool>> ToggleProviderAsync(long id, bool isActive, CancellationToken ct = default);

    Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateVehicleAsync(CreateVehicleDto dto, CancellationToken ct = default);
    Task<Result<bool>> ToggleVehicleAsync(long id, bool isActive, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryDto>> ListDeliveriesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateDeliveryAsync(CreateDeliveryDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateDeliveryStatusAsync(long id, UpdateDeliveryStatusDto dto, CancellationToken ct = default);
}
