using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class DdlItemTenant : TenantEntity
{
    public string CatalogKey { get; set; } = "";
    public string Code { get; set; } = "";
    public string LabelOverride { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
