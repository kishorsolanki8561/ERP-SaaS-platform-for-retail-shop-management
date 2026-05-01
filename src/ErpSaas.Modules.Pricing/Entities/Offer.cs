using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Pricing.Entities;

[Auditable("Pricing.Offer")]
public class Offer : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public OfferType Type { get; set; }
    public string? RulesJson { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
