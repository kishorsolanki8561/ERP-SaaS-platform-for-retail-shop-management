#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using BranchEntity = ErpSaas.Infrastructure.Data.Entities.Identity.Branch;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Metering;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class AdminService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    ITokenService tokenService,
    INotificationService notifications,
    ILogger<AdminService> logger,
    IUsageMeterService? usageMeter = null)
    : BaseService<PlatformDbContext>(db, errorLogger), IAdminService
{
    public Task<ShopProfileDto?> GetShopProfileAsync(CancellationToken ct = default)
        => db.Shops
            .Where(s => s.Id == tenant.ShopId && !s.IsDeleted)
            .Select(s => (ShopProfileDto?)new ShopProfileDto(
                s.ShopCode, s.LegalName, s.TradeName, s.GstNumber,
                s.AddressLine1, s.AddressLine2, s.City,
                s.StateCode, s.PinCode, s.CurrencyCode, s.TimeZone))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<bool>> UpdateShopProfileAsync(
        UpdateShopProfileDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.UpdateShopProfile", async () =>
        {
            var shop = await db.Shops
                .FirstOrDefaultAsync(s => s.Id == tenant.ShopId && !s.IsDeleted, ct);

            if (shop is null)
                return Result<bool>.NotFound(Errors.Admin.ShopNotFound);

            shop.LegalName    = dto.LegalName;
            shop.TradeName    = dto.TradeName;
            shop.GstNumber    = dto.GstNumber;
            shop.AddressLine1 = dto.AddressLine1;
            shop.AddressLine2 = dto.AddressLine2;
            shop.City         = dto.City;
            shop.StateCode    = dto.StateCode;
            shop.PinCode      = dto.PinCode;
            shop.CurrencyCode = dto.CurrencyCode;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(
        int pageNumber, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.UserShops
            .Where(us => us.ShopId == tenant.ShopId && us.IsActive && !us.User.IsDeleted)
            .Select(us => us.User);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.DisplayName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.Phone != null && u.Phone.Contains(search)));

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await db.UserRoles
            .Where(ur => ur.ShopId == tenant.ShopId && userIds.Contains(ur.UserId))
            .Include(ur => ur.Role)
            .ToListAsync(ct);

        var items = users.Select(u => new AdminUserDto(
            u.Id, u.DisplayName, u.Email, u.Phone, u.IsActive,
            userRoles.Where(ur => ur.UserId == u.Id)
                     .Select(ur => ur.Role.Label)
                     .ToList()))
            .ToList();

        return new PagedResult<AdminUserDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<Result<bool>> DeactivateUserAsync(long userId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.DeactivateUser", async () =>
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);
            if (user is null) return Result<bool>.NotFound(Errors.Admin.UserNotFound);
            user.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default)
        => db.Permissions
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Select(p => (PermissionDto)new PermissionDto(p.Id, p.Code, p.Module, p.Label))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<PermissionDto>)t.Result, ct);

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.ShopId == null || r.ShopId == tenant.ShopId)
            .OrderBy(r => r.Label)
            .ToListAsync(ct);

        return roles.Select(r => new RoleDto(
            r.Id, r.Code, r.Label, r.IsSystemRole,
            r.RolePermissions.Select(rp => rp.Permission.Code).ToList()))
            .ToList();
    }

    public async Task<Result<long>> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Admin.CreateRole", async () =>
        {
            if (await db.Roles.AnyAsync(r => r.Code == dto.Code && r.ShopId == tenant.ShopId, ct))
                return Result<long>.Conflict(Errors.Admin.RoleCodeTaken);

            var role = new Role
            {
                Code = dto.Code.ToUpperInvariant(),
                Label = dto.Label,
                IsSystemRole = false,
                ShopId = tenant.ShopId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(role.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateRolePermissionsAsync(
        long roleId, UpdateRolePermissionsDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.UpdateRolePermissions", async () =>
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId
                    && (r.ShopId == null || r.ShopId == tenant.ShopId), ct);

            if (role is null) return Result<bool>.NotFound(Errors.Admin.RoleNotFound);
            if (role.IsSystemRole) return Result<bool>.Forbidden(Errors.Admin.SystemRoleReadOnly);

            var allPermissions = await db.Permissions
                .Where(p => dto.PermissionCodes.Contains(p.Code))
                .ToListAsync(ct);

            db.RolePermissions.RemoveRange(role.RolePermissions);
            foreach (var perm in allPermissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = perm.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> AssignUserRoleAsync(
        long userId, long roleId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.AssignUserRole", async () =>
        {
            if (await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.ShopId == tenant.ShopId, ct))
                return Result<bool>.Success(true); // idempotent

            db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                ShopId = tenant.ShopId,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> RemoveUserRoleAsync(
        long userId, long roleId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.RemoveUserRole", async () =>
        {
            var ur = await db.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.ShopId == tenant.ShopId, ct);
            if (ur is null) return Result<bool>.NotFound(Errors.Admin.UserRoleNotFound);
            db.UserRoles.Remove(ur);
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<long>> InviteUserAsync(InviteUserDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Admin.InviteUser", async () =>
        {
            if (usageMeter is not null)
            {
                var quota = await usageMeter.CheckQuotaAsync(MeterCodes.ActiveUsers, 1, ct);
                if (quota.IsDenied)
                    return Result<long>.Conflict(Errors.Metering.QuotaConflict(MeterCodes.ActiveUsers));
            }

            if (await db.Users.AnyAsync(u => u.Email == dto.Email, ct))
                return Result<long>.Conflict(Errors.Admin.UserNotFound);

            var user = new User
            {
                Email      = dto.Email,
                Phone      = dto.Phone,
                DisplayName = dto.DisplayName,
                PasswordHash = "",     // set on AcceptInvite
                IsActive    = false,   // activated when invite is accepted
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            db.UserShops.Add(new UserShop { UserId = user.Id, ShopId = tenant.ShopId, IsActive = true, CreatedAtUtc = DateTime.UtcNow });

            if (dto.RoleId.HasValue)
                db.UserRoles.Add(new UserRole { UserId = user.Id, ShopId = tenant.ShopId, RoleId = dto.RoleId.Value, CreatedAtUtc = DateTime.UtcNow });

            var rawToken = tokenService.GenerateRefreshToken();
            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(rawToken),
                Purpose = SecurityTokenPurpose.Invite,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            });
            await db.SaveChangesAsync(ct);

            try
            {
                await notifications.EnqueueAsync(tenant.ShopId, NotificationChannel.Email, dto.Email,
                    Constants.NotificationCodes.UserInvite,
                    new Dictionary<string, string> { ["Name"] = dto.DisplayName, ["InviteLink"] = rawToken },
                    ct: ct);
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to send invite email to {Email}", dto.Email); }

            if (usageMeter is not null)
                await usageMeter.IncrementAsync(MeterCodes.ActiveUsers, 1, "User", user.Id, ct: ct);

            return Result<long>.Success(user.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ResendInviteAsync(long userId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.ResendInvite", async () =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<bool>.NotFound(Errors.Admin.UserNotFound);

            // Expire old invite tokens
            var oldTokens = await db.UserSecurityTokens
                .Where(t => t.UserId == userId && t.Purpose == SecurityTokenPurpose.Invite && t.ConsumedAtUtc == null)
                .ToListAsync(ct);
            oldTokens.ForEach(t => t.ConsumedAtUtc = DateTime.UtcNow);

            var rawToken = tokenService.GenerateRefreshToken();
            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(rawToken),
                Purpose = SecurityTokenPurpose.Invite,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            });
            await db.SaveChangesAsync(ct);

            if (user.Email is not null)
            {
                try
                {
                    await notifications.EnqueueAsync(tenant.ShopId, NotificationChannel.Email, user.Email,
                        Constants.NotificationCodes.UserInvite,
                        new Dictionary<string, string> { ["Name"] = user.DisplayName, ["InviteLink"] = rawToken },
                        ct: ct);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to resend invite to {Email}", user.Email); }
            }

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ForceResetPasswordAsync(long userId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.ForceResetPassword", async () =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<bool>.NotFound(Errors.Admin.UserNotFound);

            var rawToken = tokenService.GenerateRefreshToken();
            db.UserSecurityTokens.Add(new UserSecurityToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(rawToken),
                Purpose = SecurityTokenPurpose.PasswordReset,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24),
            });
            await db.SaveChangesAsync(ct);

            if (user.Email is not null)
            {
                try
                {
                    await notifications.EnqueueAsync(tenant.ShopId, NotificationChannel.Email, user.Email,
                        Constants.NotificationCodes.PasswordReset,
                        new Dictionary<string, string> { ["Name"] = user.DisplayName, ["ResetLink"] = rawToken },
                        ct: ct);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to send force-reset email to {Email}", user.Email); }
            }

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UnlockUserAsync(long userId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.UnlockUser", async () =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return Result<bool>.NotFound(Errors.Admin.UserNotFound);
            user.FailedLoginCount = 0;
            user.LockoutUntilUtc = null;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    // ── Branches ─────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<BranchDto>> ListBranchesAsync(CancellationToken ct = default)
        => db.Branches
            .Where(b => b.ShopId == tenant.ShopId && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => (BranchDto)new BranchDto(b.Id, b.Name, b.City, b.Phone, b.IsActive, b.IsHeadOffice))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<BranchDto>)t.Result);

    public async Task<Result<long>> CreateBranchAsync(CreateBranchDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Admin.CreateBranch", async () =>
        {
            var branch = new BranchEntity
            {
                ShopId       = tenant.ShopId,
                Name         = dto.Name,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City         = dto.City,
                StateCode    = dto.StateCode,
                PinCode      = dto.PinCode,
                Phone        = dto.Phone,
                GstNumber    = dto.GstNumber,
                IsHeadOffice = dto.IsHeadOffice,
                IsActive     = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Branches.Add(branch);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(branch.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> UpdateBranchAsync(long branchId, UpdateBranchDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.UpdateBranch", async () =>
        {
            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.ShopId == tenant.ShopId, ct);
            if (branch is null) return Result<bool>.NotFound(Errors.Admin.ShopNotFound);
            branch.Name         = dto.Name;
            branch.AddressLine1 = dto.AddressLine1;
            branch.AddressLine2 = dto.AddressLine2;
            branch.City         = dto.City;
            branch.StateCode    = dto.StateCode;
            branch.PinCode      = dto.PinCode;
            branch.Phone        = dto.Phone;
            branch.GstNumber    = dto.GstNumber;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DeactivateBranchAsync(long branchId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Admin.DeactivateBranch", async () =>
        {
            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.ShopId == tenant.ShopId, ct);
            if (branch is null) return Result<bool>.NotFound(Errors.Admin.ShopNotFound);
            branch.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
}
