namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class DdlItem
{
    public long Id { get; set; }
    public long CatalogId { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public string? ParentCode { get; set; }
    public bool IsActive { get; set; } = true;

    public DdlCatalog Catalog { get; set; } = null!;
}
