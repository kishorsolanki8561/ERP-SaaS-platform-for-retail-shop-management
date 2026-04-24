using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class Role : BaseEntity
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public bool IsSystemRole { get; set; }
    public long? ShopId { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
