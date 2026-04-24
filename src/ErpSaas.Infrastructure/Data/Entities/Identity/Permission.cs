using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class Permission : BaseEntity
{
    public string Code { get; set; } = "";
    public string Module { get; set; } = "";
    public string Label { get; set; } = "";

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
