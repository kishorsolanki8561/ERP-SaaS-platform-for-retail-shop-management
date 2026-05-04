using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AttendanceDto(
    long Id, long EmployeeId, string EmployeeName, DateTime Date,
    DateTime? CheckInUtc, DateTime? CheckOutUtc, string StatusCode, string? Notes);

public record CheckInDto(long EmployeeId, double? Latitude, double? Longitude, string? Notes);

public record BulkAttendanceDto(DateTime Date, IReadOnlyList<BulkAttendanceEntry> Entries);

public record BulkAttendanceEntry(long EmployeeId, string StatusCode, string? Notes);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IAttendanceService
{
    Task<Result<long>> CheckInAsync(CheckInDto dto, CancellationToken ct = default);
    Task<Result<bool>> CheckOutAsync(long employeeId, CancellationToken ct = default);
    Task<Result<bool>> BulkMarkAsync(BulkAttendanceDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceDto>> ListAsync(int year, int month, CancellationToken ct = default);
}
