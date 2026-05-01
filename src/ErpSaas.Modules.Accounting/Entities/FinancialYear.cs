using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.FinancialYear")]
public class FinancialYear : TenantEntity
{
    public int StartYear { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public long? ClosedByUserId { get; set; }

    public ICollection<Voucher> Vouchers { get; set; } = [];
}
