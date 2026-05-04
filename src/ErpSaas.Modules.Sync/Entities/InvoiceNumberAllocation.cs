using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Sync.Entities;

public class InvoiceNumberAllocation : TenantEntity
{
    public string DeviceId { get; set; } = default!;
    public long BranchId { get; set; }
    public int FinancialYear { get; set; }
    public long RangeStart { get; set; }
    public long RangeEnd { get; set; }
    public long LastUsed { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public InvoiceNumberAllocationStatus Status { get; set; } = InvoiceNumberAllocationStatus.Active;
}
