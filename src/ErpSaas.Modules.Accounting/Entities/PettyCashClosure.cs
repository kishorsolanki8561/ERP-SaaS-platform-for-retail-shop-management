using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Accounting.Entities;

public class PettyCashClosure : TenantEntity
{
    public DateTime ClosureDate { get; set; }
    public decimal ExpectedBalance { get; set; }
    public decimal CountedBalance { get; set; }
    public decimal Variance { get; set; }
    public string Narration { get; set; } = default!;
    public long? VarianceVoucherId { get; set; }
}
