using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Verticals.Entities;

public class ShopVertical : TenantEntity
{
    public long VerticalPackId { get; set; }
    public string VerticalPackCode { get; set; } = default!;
    public DateTime AppliedAtUtc { get; set; }
    public long AppliedByUserId { get; set; }
}
