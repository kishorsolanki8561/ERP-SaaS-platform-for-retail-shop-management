using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public enum SecurityTokenPurpose
{
    Invite,
    PasswordReset,
    EmailVerify,
    TotpChallenge,
    RefreshToken
}

public class UserSecurityToken : BaseEntity
{
    public long UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public SecurityTokenPurpose Purpose { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
