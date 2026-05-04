using System.Security.Cryptography;
using System.Text;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.ApiAccess.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.ApiAccess.Services;

public sealed class ShopApiKeyService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IShopApiKeyService
{
    public async Task<Result<ApiKeyCreatedResult>> CreateAsync(CreateApiKeyDto dto, long createdByUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.CreateApiKey", async () =>
        {
            var rawKey = GenerateRawKey();
            var hash = HashKey(rawKey);
            var prefix = rawKey[..12];

            var key = new ShopApiKey
            {
                ShopId = tenant.ShopId,
                KeyPrefix = prefix,
                KeyHashSha256 = hash,
                Name = dto.Name,
                ScopesCsv = dto.ScopesCsv,
                ExpiresAtUtc = dto.ExpiresAtUtc,
                IsActive = true,
                CreatedByUserId = createdByUserId,
                RateLimitPerMinute = dto.RateLimitPerMinute,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<ShopApiKey>().Add(key);
            await _db.SaveChangesAsync(ct);

            return Result<ApiKeyCreatedResult>.Success(new ApiKeyCreatedResult(key.Id, rawKey, prefix));
        }, ct, useTransaction: true);
    }

    public async Task<Result<IReadOnlyList<ApiKeyListItem>>> ListAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.ListApiKeys", async () =>
        {
            var keys = await _db.Set<ShopApiKey>()
                .OrderByDescending(k => k.CreatedAtUtc)
                .Select(k => new ApiKeyListItem(
                    k.Id, k.KeyPrefix, k.Name, k.ScopesCsv, k.IsActive,
                    k.ExpiresAtUtc, k.LastUsedAtUtc, k.CreatedAtUtc))
                .ToListAsync(ct);

            return Result<IReadOnlyList<ApiKeyListItem>>.Success(keys);
        }, ct);
    }

    public async Task<Result<bool>> RevokeAsync(long id, RevokeApiKeyDto dto, long revokedByUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.RevokeApiKey", async () =>
        {
            var key = await _db.Set<ShopApiKey>().FirstOrDefaultAsync(k => k.Id == id, ct);
            if (key is null) return Result<bool>.NotFound(Errors.ApiAccess.KeyNotFound);
            if (!key.IsActive) return Result<bool>.Conflict(Errors.ApiAccess.KeyAlreadyRevoked);

            key.IsActive = false;
            key.RevokedAtUtc = DateTime.UtcNow;
            key.RevokedByUserId = revokedByUserId;
            key.RevokedReason = dto.Reason;
            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<ApiKeyCreatedResult>> RotateAsync(long id, long rotatedByUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync("ApiAccess.RotateApiKey", async () =>
        {
            var key = await _db.Set<ShopApiKey>().FirstOrDefaultAsync(k => k.Id == id, ct);
            if (key is null) return Result<ApiKeyCreatedResult>.NotFound(Errors.ApiAccess.KeyNotFound);
            if (!key.IsActive) return Result<ApiKeyCreatedResult>.Conflict(Errors.ApiAccess.KeyAlreadyRevoked);

            // Revoke old key
            key.IsActive = false;
            key.RevokedAtUtc = DateTime.UtcNow;
            key.RevokedByUserId = rotatedByUserId;
            key.RevokedReason = "Rotated";

            // Issue new key
            var rawKey = GenerateRawKey();
            var newKey = new ShopApiKey
            {
                ShopId = tenant.ShopId,
                KeyPrefix = rawKey[..12],
                KeyHashSha256 = HashKey(rawKey),
                Name = key.Name,
                ScopesCsv = key.ScopesCsv,
                ExpiresAtUtc = key.ExpiresAtUtc,
                IsActive = true,
                CreatedByUserId = rotatedByUserId,
                RateLimitPerMinute = key.RateLimitPerMinute,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<ShopApiKey>().Add(newKey);
            await _db.SaveChangesAsync(ct);

            return Result<ApiKeyCreatedResult>.Success(new ApiKeyCreatedResult(newKey.Id, rawKey, newKey.KeyPrefix));
        }, ct, useTransaction: true);
    }

    private static string GenerateRawKey()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(30);
        return "sk_live_" + Convert.ToBase64String(randomBytes).Replace("+", "A").Replace("/", "B").Replace("=", "");
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
