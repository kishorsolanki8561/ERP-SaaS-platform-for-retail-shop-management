namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class UserPermissionOverride
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ShopId { get; set; }
    public string PermissionCode { get; set; } = "";
    public bool IsGranted { get; set; }
    public long SetByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
}
