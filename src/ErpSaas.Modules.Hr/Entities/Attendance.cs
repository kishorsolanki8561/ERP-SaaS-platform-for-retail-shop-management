using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hr.Entities;

[Auditable("HR.Attendance")]
public class Attendance : TenantEntity
{
    public long EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public string StatusCode { get; set; } = default!;
    public string? Notes { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }

    public Employee Employee { get; set; } = default!;
}
