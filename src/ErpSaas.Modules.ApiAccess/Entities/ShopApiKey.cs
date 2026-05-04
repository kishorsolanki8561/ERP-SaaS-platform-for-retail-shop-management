using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.ApiAccess.Entities;

[Auditable("ApiAccess.ShopApiKey")]
public sealed class ShopApiKey : TenantEntity
{
    public string KeyPrefix { get; set; } = default!;
    public string KeyHashSha256 { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ScopesCsv { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public string? LastUsedIp { get; set; }
    public bool IsActive { get; set; }
public DateTime? RevokedAtUtc { get; set; }
    public long? RevokedByUserId { get; set; }
    public string? RevokedReason { get; set; }
    public int RateLimitPerMinute { get; set; } = 600;
}
