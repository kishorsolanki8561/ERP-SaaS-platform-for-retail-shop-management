using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.ApiAccess.Services;

public record CreateApiKeyDto(string Name, string? ScopesCsv, DateTime? ExpiresAtUtc, int RateLimitPerMinute = 600);
public record ApiKeyListItem(long Id, string KeyPrefix, string Name, string? ScopesCsv, bool IsActive,
    DateTime? ExpiresAtUtc, DateTime? LastUsedAtUtc, DateTime CreatedAtUtc);
public record ApiKeyCreatedResult(long Id, string RawKey, string KeyPrefix);
public record RevokeApiKeyDto(string? Reason);

public interface IShopApiKeyService
{
    Task<Result<ApiKeyCreatedResult>> CreateAsync(CreateApiKeyDto dto, long createdByUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ApiKeyListItem>>> ListAsync(CancellationToken ct = default);
    Task<Result<bool>> RevokeAsync(long id, RevokeApiKeyDto dto, long revokedByUserId, CancellationToken ct = default);
    Task<Result<ApiKeyCreatedResult>> RotateAsync(long id, long rotatedByUserId, CancellationToken ct = default);
}
