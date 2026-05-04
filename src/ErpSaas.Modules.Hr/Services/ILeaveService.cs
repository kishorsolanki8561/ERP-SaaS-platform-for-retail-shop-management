using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record LeaveTypeDto(long Id, string Code, string Name, int DefaultAnnualQuota, bool IsPaid, bool IsCarryForward, bool IsActive);

public record CreateLeaveTypeDto(string Code, string Name, int DefaultAnnualQuota, bool IsPaid, bool IsCarryForward);

public record LeaveRequestDto(
    long Id, long EmployeeId, string EmployeeName, string LeaveTypeCode,
    DateTime StartDate, DateTime EndDate, decimal Days, string Reason,
    LeaveStatus Status, DateTime? ApprovedAtUtc);

public record CreateLeaveRequestDto(long EmployeeId, long LeaveTypeId, DateTime StartDate, DateTime EndDate, decimal Days, string Reason);

public record LeaveBalanceDto(long LeaveTypeId, string LeaveTypeCode, string LeaveTypeName, decimal EntitledDays, decimal UsedDays, decimal CarryForwardDays, decimal AvailableDays);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ILeaveService
{
    Task<Result<long>> CreateLeaveTypeAsync(CreateLeaveTypeDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveTypeDto>> ListLeaveTypesAsync(CancellationToken ct = default);
    Task<Result<long>> RequestLeaveAsync(CreateLeaveRequestDto dto, CancellationToken ct = default);
    Task<Result<bool>> ApproveAsync(long id, long approverUserId, CancellationToken ct = default);
    Task<Result<bool>> RejectAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveRequestDto>> ListRequestsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(long employeeId, int year, CancellationToken ct = default);
}
