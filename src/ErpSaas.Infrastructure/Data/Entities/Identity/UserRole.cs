using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class UserRole : BaseEntity
{
    public long UserId { get; set; }
    public long ShopId { get; set; }
    public long RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
