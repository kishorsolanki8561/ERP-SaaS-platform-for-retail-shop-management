using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class UserShop : BaseEntity
{
    public long UserId { get; set; }
    public long ShopId { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
}
