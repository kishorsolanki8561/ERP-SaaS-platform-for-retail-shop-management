using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Menu;

public class MenuItemTenantOverride : TenantEntity
{
    public string MenuItemCode { get; set; } = "";
    public string? LabelOverride { get; set; }
    public bool IsHidden { get; set; }
    public int? SortOrderOverride { get; set; }
}
