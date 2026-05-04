using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record PayrollDto(
    long Id, long EmployeeId, string EmployeeName, int Year, int Month,
    decimal GrossEarnings, decimal TotalDeductions, decimal NetPay,
    int PresentDays, int AbsentDays, int LeaveDays,
    PayrollStatus Status, DateTime? PaidAtUtc);

public record GeneratePayrollDto(long EmployeeId, int Year, int Month);

public record PayslipDto(
    long PayrollId, string EmployeeName, string EmployeeCode,
    int Year, int Month, decimal GrossEarnings,
    decimal TotalDeductions, decimal NetPay,
    int PresentDays, int AbsentDays, int LeaveDays,
    string DetailsJson);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IPayrollService
{
    Task<Result<long>> GenerateAsync(GeneratePayrollDto dto, CancellationToken ct = default);
    Task<Result<bool>> ApproveAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> PayAsync(long id, CancellationToken ct = default);
    Task<PayslipDto?> GetPayslipAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollDto>> ListAsync(int year, int month, CancellationToken ct = default);
}
