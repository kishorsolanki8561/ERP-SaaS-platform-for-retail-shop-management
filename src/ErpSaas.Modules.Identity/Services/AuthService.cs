#pragma warning disable CS9107
using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class AuthService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ITokenService tokenService,
    IPermissionService permissionService,
    IConfiguration configuration,
    INotificationService notifications,
    ILogger<AuthService> logger)
    : BaseService<PlatformDbContext>(db, errorLogger), IAuthService
{
    private int MaxFailedAttempts => int.Parse(
        configuration[Constants.Security.MaxFailedLoginAttemptsKey]
        ?? Constants.Security.DefaultMaxFailedLoginAttempts.ToString());

    private TimeSpan LockoutDuration => TimeSpan.FromMinutes(int.Parse(
        configuration[Constants.Security.LockoutDurationMinutesKey]
        ?? Constants.Security.DefaultLockoutDurationMinutes.ToString()));

    private int TotpChallengeMinutes => int.Parse(
        configuration[Constants.Security.TotpChallengeMinutesKey]
        ?? Constants.Security.DefaultTotpChallengeMinutes.ToString());

    private int RefreshTokenDays => int.Parse(
        configuration[Constants.Security.RefreshTokenDaysKey]
        ?? Constants.Security.DefaultRefreshTokenDays.ToString());

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
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(TotpChallengeMinutes)
                });
                await db.SaveChangesAsync(ct);
                return Result<object>.Success((object)new TotpChallengeResponse(challengeRaw));
            }

            var shopId = await GetDefaultShopIdAsync(user.Id, ct);
            var perms = await permissionService.GetPermissionCodesAsync(user.Id, shopId, ct);
            var feats = await permissionService.GetFeatureCodesAsync(shopId, ct);

            var pair = tokenService.GenerateTokenPair(user.Id, shopId, user.DisplayName, user.Email, perms, feats, user.IsPlatformAdmin);

            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(pair.RefreshToken),
                Purpose = SecurityTokenPurpose.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
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

            var pair = tokenService.GenerateTokenPair(user.Id, shopId, user.DisplayName, user.Email, perms, feats, user.IsPlatformAdmin);

            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(pair.RefreshToken),
                Purpose = SecurityTokenPurpose.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
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

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Identity.ForgotPassword", async () =>
        {
            var user = await FindUserAsync(request.Identifier, ct);
            // Always return success to prevent user enumeration
            if (user is null || !user.IsActive) return Result<bool>.Success(true);

            var rawToken = tokenService.GenerateRefreshToken();
            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(rawToken),
                Purpose = SecurityTokenPurpose.PasswordReset,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(2),
            });
            await db.SaveChangesAsync(ct);

            if (user.Email is not null)
            {
                try
                {
                    await notifications.EnqueueAsync(0, NotificationChannel.Email, user.Email,
                        Constants.NotificationCodes.PasswordReset,
                        new Dictionary<string, string>
                        {
                            ["Name"] = user.DisplayName,
                            ["ResetLink"] = rawToken,
                        }, ct: ct);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email); }
            }

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Identity.ResetPassword", async () =>
        {
            var hash = tokenService.HashToken(request.Token);
            var token = await db.UserSecurityTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == hash &&
                    t.Purpose == SecurityTokenPurpose.PasswordReset &&
                    t.ConsumedAtUtc == null &&
                    t.ExpiresAtUtc > DateTime.UtcNow, ct);

            if (token is null)
                return Result<bool>.Forbidden(Errors.Auth.InvalidRefreshToken);

            var workFactor = int.Parse(configuration[Constants.Security.BcryptWorkFactorKey]
                ?? Constants.Security.DefaultBcryptWorkFactor.ToString());

            token.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor);
            token.User.FailedLoginCount = 0;
            token.User.LockoutUntilUtc = null;
            token.ConsumedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<LoginResponse>> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default)
        => await ExecuteAsync<LoginResponse>("Identity.AcceptInvite", async () =>
        {
            var hash = tokenService.HashToken(request.Token);
            var token = await db.UserSecurityTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == hash &&
                    t.Purpose == SecurityTokenPurpose.Invite &&
                    t.ConsumedAtUtc == null &&
                    t.ExpiresAtUtc > DateTime.UtcNow, ct);

            if (token is null)
                return Result<LoginResponse>.Forbidden(Errors.Auth.InvalidRefreshToken);

            var workFactor = int.Parse(configuration[Constants.Security.BcryptWorkFactorKey]
                ?? Constants.Security.DefaultBcryptWorkFactor.ToString());

            token.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor);
            token.User.IsActive = true;
            token.ConsumedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            // Auto-login after accepting invite
            var shopId = await GetDefaultShopIdAsync(token.User.Id, ct);
            var perms = await permissionService.GetPermissionCodesAsync(token.User.Id, shopId, ct);
            var feats = await permissionService.GetFeatureCodesAsync(shopId, ct);

            var pair = tokenService.GenerateTokenPair(token.User.Id, shopId,
                token.User.DisplayName, token.User.Email, perms, feats, token.User.IsPlatformAdmin);

            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = token.User.Id,
                TokenHash = tokenService.HashToken(pair.RefreshToken),
                Purpose = SecurityTokenPurpose.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
            });
            await db.SaveChangesAsync(ct);

            return Result<LoginResponse>.Success(new LoginResponse(pair.AccessToken, pair.RefreshToken, pair.AccessExpiresAtUtc));
        }, ct, useTransaction: true);

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
