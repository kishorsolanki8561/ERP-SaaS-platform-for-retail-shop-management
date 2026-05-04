namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class ShopFeatureOverride
{
    public long Id { get; set; }
    public long ShopId { get; set; }
    public string FeatureCode { get; set; } = "";
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Shop Shop { get; set; } = null!;
}
