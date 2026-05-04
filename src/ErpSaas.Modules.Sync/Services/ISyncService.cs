using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Sync.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record RegisterDeviceDto(
    string DeviceId,
    long BranchId,
    long AssignedUserId,
    string DeviceTypeCode,
    string PlatformInfo,
    string AppVersion);

public sealed record DeviceDto(
    long Id,
    string DeviceId,
    long BranchId,
    long AssignedUserId,
    string Type,
    string PlatformInfo,
    string AppVersion,
    DateTime LastSeenAtUtc,
    DateTime? LastSyncedAtUtc,
    bool IsActive);

public sealed record HeartbeatDto(
    string DeviceId,
    string AppVersion,
    string PlatformInfo);

public sealed record HeartbeatResultDto(
    bool CatalogStale,
    bool PricingStale,
    bool CustomersStale,
    DateTime ServerUtcNow);

public sealed record OfflineCommandDto(
    Guid ClientCommandId,
    string DeviceId,
    string CommandType,
    string PayloadJson,
    DateTime ClientTimestampUtc);

public sealed record CommandResultDto(
    Guid ClientCommandId,
    bool Success,
    long? ResultingEntityId,
    string? Warning,
    string? RejectionReason);

public sealed record SyncCommandsBatchDto(
    IReadOnlyList<OfflineCommandDto> Commands);

public sealed record SyncCommandsBatchResultDto(
    IReadOnlyList<CommandResultDto> Results);

public sealed record AllocateInvoiceRangeDto(
    string DeviceId,
    long BranchId,
    int RangeSize);

public sealed record InvoiceRangeDto(
    long AllocationId,
    long RangeStart,
    long RangeEnd,
    int FinancialYear);

public sealed record ReleaseInvoiceRangeDto(long HighestUsed);

public sealed record SyncExceptionDto(
    long Id,
    string DeviceId,
    string CommandType,
    DateTime ClientTimestampUtc,
    string Status,
    string? RejectionReason,
    string? WarningNote);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IDeviceService
{
    Task<Result<DeviceDto>> RegisterAsync(RegisterDeviceDto dto, CancellationToken ct = default);
    Task<Result<HeartbeatResultDto>> HeartbeatAsync(long deviceId, HeartbeatDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken ct = default);
    Task<Result<bool>> DeactivateAsync(long deviceId, CancellationToken ct = default);
}

public interface ISyncService
{
    Task<SyncCommandsBatchResultDto> ProcessCommandsAsync(SyncCommandsBatchDto batch, CancellationToken ct = default);
    Task<Result<InvoiceRangeDto>> AllocateInvoiceRangeAsync(AllocateInvoiceRangeDto dto, CancellationToken ct = default);
    Task<Result<bool>> ReleaseInvoiceRangeAsync(long allocationId, ReleaseInvoiceRangeDto dto, CancellationToken ct = default);
    Task<(IReadOnlyList<SyncExceptionDto> Items, int TotalCount)> ListExceptionsAsync(
        int pageNumber, int pageSize, CancellationToken ct = default);
}
