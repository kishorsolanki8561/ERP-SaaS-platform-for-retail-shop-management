#pragma warning disable CS9107
using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

public sealed class AuthService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ITokenService tokenService,
    IPermissionService permissionService)
    : BaseService<PlatformDbContext>(db, errorLogger), IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<Result<object>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<object>("Identity.Login", async () =>
        {
            var user = await FindUserAsync(request.Identifier, ct);
            if (user is null)
                return Result<object>.NotFound(Errors.Auth.UserNotFound);

            if (!user.IsActive)
                return Result<object>.Forbidden(Errors.Auth.AccountInactive);

            if (user.LockoutUntilUtc.HasValue && user.LockoutUntilUtc > DateTime.UtcNow)
                return Result<object>.Forbidden(Errors.Auth.AccountLockedUntil(user.LockoutUntilUtc.Value));

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                    user.LockoutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);

                await db.SaveChangesAsync(ct);
                return Result<object>.Forbidden(Errors.Auth.InvalidCredentials);
            }

            // Reset lockout on success
            user.FailedLoginCount = 0;
            user.LockoutUntilUtc = null;
            user.LastLoginAtUtc = DateTime.UtcNow;

            if (user.IsTotpEnabled)
            {
                var challengeRaw = tokenService.GenerateRefreshToken();
                var challengeHash = tokenService.HashToken(challengeRaw);
                db.UserSecurityTokens.Add(new UserSecurityToken
                {
                    UserId = user.Id,
                    TokenHash = challengeHash,
                    Purpose = SecurityTokenPurpose.TotpChallenge,
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
                });
                await db.SaveChangesAsync(ct);
                return Result<object>.Success((object)new TotpChallengeResponse(challengeRaw));
            }

            var shopId = await GetDefaultShopIdAsync(user.Id, ct);
            var perms = await permissionService.GetPermissionCodesAsync(user.Id, shopId, ct);
            var feats = await permissionService.GetFeatureCodesAsync(shopId, ct);

            var pair = tokenService.GenerateTokenPair(user.Id, shopId, user.DisplayName, user.Email, perms, feats);

            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(pair.RefreshToken),
                Purpose = SecurityTokenPurpose.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
            });

            await db.SaveChangesAsync(ct);
            return Result<object>.Success((object)new LoginResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAtUtc));
        }, ct, useTransaction: true);
    }

    public async Task<Result<LoginResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        return await ExecuteAsync<LoginResponse>("Identity.Refresh", async () =>
        {
            var hash = tokenService.HashToken(refreshToken);
            var stored = await db.UserSecurityTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == hash &&
                    t.Purpose == SecurityTokenPurpose.RefreshToken &&
                    t.ConsumedAtUtc == null &&
                    t.ExpiresAtUtc > DateTime.UtcNow, ct);

            if (stored is null)
                return Result<LoginResponse>.Forbidden(Errors.Auth.InvalidRefreshToken);

            stored.ConsumedAtUtc = DateTime.UtcNow;

            var user = stored.User;
            if (!user.IsActive)
                return Result<LoginResponse>.Forbidden(Errors.Auth.AccountInactive);

            var shopId = await GetDefaultShopIdAsync(user.Id, ct);
            var perms = await permissionService.GetPermissionCodesAsync(user.Id, shopId, ct);
            var feats = await permissionService.GetFeatureCodesAsync(shopId, ct);

            var pair = tokenService.GenerateTokenPair(user.Id, shopId, user.DisplayName, user.Email, perms, feats);

            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(pair.RefreshToken),
                Purpose = SecurityTokenPurpose.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
            });

            await db.SaveChangesAsync(ct);
            return Result<LoginResponse>.Success(new LoginResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAtUtc));
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        return await ExecuteAsync<bool>("Identity.Logout", async () =>
        {
            var hash = tokenService.HashToken(refreshToken);
            var stored = await db.UserSecurityTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == SecurityTokenPurpose.RefreshToken, ct);

            if (stored is not null)
            {
                stored.ConsumedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            return Result<bool>.Success(true);
        }, ct);
    }

    private async Task<User?> FindUserAsync(string identifier, CancellationToken ct)
    {
        if (identifier.Contains('@'))
            return await db.Users.FirstOrDefaultAsync(u => u.Email == identifier, ct);

        if (identifier.All(char.IsDigit))
            return await db.Users.FirstOrDefaultAsync(u => u.Phone == identifier, ct);

        return await db.Users.FirstOrDefaultAsync(u => u.Username == identifier, ct);
    }

    private async Task<long> GetDefaultShopIdAsync(long userId, CancellationToken ct)
    {
        var shopId = await db.UserShops
            .Where(us => us.UserId == userId && us.IsActive)
            .Select(us => (long?)us.ShopId)
            .FirstOrDefaultAsync(ct);

        return shopId ?? 0;
    }
}
