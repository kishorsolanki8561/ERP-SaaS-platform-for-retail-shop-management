using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hardware.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record DeviceProfileDto(
    long Id,
    string DeviceId,
    DeviceClass Class,
    string VendorCode,
    string ModelCode,
    string ConnectionJson,
    string Role,
    bool IsDefault,
    bool IsActive,
    DateTime? LastUsedAtUtc);

public record CreateDeviceProfileDto(
    string DeviceId,
    DeviceClass Class,
    string VendorCode,
    string ModelCode,
    string ConnectionJson,
    string Role,
    bool IsDefault = false);

public record UpdateDeviceProfileDto(
    string? VendorCode,
    string? ModelCode,
    string? ConnectionJson,
    string? Role,
    bool? IsDefault,
    bool? IsActive);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IDeviceProfileService
{
    Task<Result<long>> CreateAsync(CreateDeviceProfileDto dto, CancellationToken ct = default);

    Task<Result<bool>> UpdateAsync(long id, UpdateDeviceProfileDto dto, CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default);

    Task<DeviceProfileDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<DeviceProfileDto>> ListAsync(CancellationToken ct = default);
}
