using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class User : BaseEntity
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsPlatformAdmin { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public string? TotpSecretEncrypted { get; set; }
    public bool IsTotpEnabled { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<UserShop> UserShops { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserSecurityToken> SecurityTokens { get; set; } = [];
}
