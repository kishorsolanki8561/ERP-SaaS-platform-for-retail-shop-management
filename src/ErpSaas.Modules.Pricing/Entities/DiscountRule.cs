using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Pricing.Entities;

[Auditable("Pricing.DiscountRule")]
public class DiscountRule : TenantEntity
{
    public string Name { get; set; } = default!;
    public string DiscountTypeCode { get; set; } = default!;
    public DiscountScope Scope { get; set; }
    public long? ProductId { get; set; }
    public long? CategoryId { get; set; }
    public long? CustomerTypeId { get; set; }
    public decimal? PercentValue { get; set; }
    public decimal? FixedValue { get; set; }
    public int? BuyQty { get; set; }
    public int? GetQty { get; set; }
    public decimal? MinInvoiceAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Priority { get; set; }
    public bool IsStackable { get; set; }
    public bool IsActive { get; set; } = true;
}
