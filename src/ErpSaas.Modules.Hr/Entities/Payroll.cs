using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Entities;

[Auditable("HR.Payroll")]
public class Payroll : TenantEntity
{
    public long EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeaveDays { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public DateTime? PaidAtUtc { get; set; }
    public long? PaymentVoucherId { get; set; }
    public string DetailsJson { get; set; } = "{}";

    public Employee Employee { get; set; } = default!;
}
