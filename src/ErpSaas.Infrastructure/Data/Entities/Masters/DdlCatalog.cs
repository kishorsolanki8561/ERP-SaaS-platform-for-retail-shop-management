namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class DdlCatalog
{
    public long Id { get; set; }
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public ICollection<DdlItem> Items { get; set; } = [];
}
