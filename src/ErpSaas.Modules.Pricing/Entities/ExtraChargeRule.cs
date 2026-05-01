using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Pricing.Entities;

[Auditable("Pricing.ExtraChargeRule")]
public class ExtraChargeRule : TenantEntity
{
    public string Name { get; set; } = default!;
    public ChargeType Type { get; set; }
    public decimal Value { get; set; }
    public bool IsTaxable { get; set; }
    public decimal? GstRate { get; set; }
    public bool IsActive { get; set; } = true;
}
