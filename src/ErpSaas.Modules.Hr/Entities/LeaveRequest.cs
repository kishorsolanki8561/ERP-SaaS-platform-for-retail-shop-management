using ErpSaas.Modules.Hr.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Entities;

[Auditable("HR.LeaveRequest")]
public class LeaveRequest : TenantEntity
{
    public long EmployeeId { get; set; }
    public long LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string Reason { get; set; } = default!;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    public Employee Employee { get; set; } = default!;
    public LeaveType LeaveType { get; set; } = default!;
}
