using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Menu;

public enum MenuItemKind { Group, Submenu, Page }

public class MenuItem : BaseEntity
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public MenuItemKind Kind { get; set; }
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? RequiredPermission { get; set; }
    public string? RequiredFeature { get; set; }
    public bool IsActive { get; set; } = true;

    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];
}
