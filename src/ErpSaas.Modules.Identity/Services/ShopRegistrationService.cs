#pragma warning disable CS9107
using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ErpSaas.Modules.Identity.Services;

public sealed class ShopRegistrationService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    IConfiguration configuration,
    IShopOnboardingService onboardingService)
    : BaseService<PlatformDbContext>(db, errorLogger), IShopRegistrationService
{
    public async Task<Result<long>> SubmitAsync(SubmitRegistrationRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<long>("Identity.SubmitRegistration", async () =>
        {
            if (await db.Shops.AnyAsync(s => s.ShopCode == request.ShopCode, ct) ||
                await db.ShopRegistrationRequests.AnyAsync(
                    r => r.ShopCode == request.ShopCode &&
                         (r.Status == RegistrationStatus.Pending || r.Status == RegistrationStatus.Approved), ct))
                return Result<long>.Conflict(Errors.Registration.ShopCodeConflict(request.ShopCode));

            if (await db.Users.AnyAsync(u => u.Email == request.AdminEmail, ct) ||
                await db.ShopRegistrationRequests.AnyAsync(
                    r => r.AdminEmail == request.AdminEmail &&
                         (r.Status == RegistrationStatus.Pending || r.Status == RegistrationStatus.Approved), ct))
                return Result<long>.Conflict(Errors.Registration.EmailConflict(request.AdminEmail));

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(
                request.Password,
                workFactor: int.Parse(
                    configuration[Constants.Security.BcryptWorkFactorKey]
                    ?? Constants.Security.DefaultBcryptWorkFactor.ToString()));

            var reg = new ShopRegistrationRequest
            {
                ShopCode           = request.ShopCode,
                LegalName          = request.LegalName,
                TradeName          = request.TradeName,
                GstNumber          = request.GstNumber,
                AdminEmail         = request.AdminEmail,
                AdminDisplayName   = request.AdminDisplayName,
                PasswordHashSnapshot = passwordHash,
                ContactPhone       = request.ContactPhone,
                Notes              = request.Notes,
                Status             = RegistrationStatus.Pending,
                CreatedAtUtc       = DateTime.UtcNow,
            };
            db.ShopRegistrationRequests.Add(reg);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(reg.Id);
        }, ct, useTransaction: false);
    }

    public async Task<(IReadOnlyList<RegistrationSummaryDto> Items, int TotalCount)> ListAsync(
        int pageNumber, int pageSize, RegistrationStatus? status, CancellationToken ct = default)
    {
        var query = db.ShopRegistrationRequests.AsNoTracking();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RegistrationSummaryDto(
                r.Id, r.ShopCode, r.LegalName, r.AdminEmail, r.ContactPhone,
                r.Status, r.CreatedAtUtc, r.ReviewedAtUtc, r.RejectionReason))
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<ShopRegistrationRequest?> GetAsync(long id, CancellationToken ct = default)
        => db.ShopRegistrationRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Result<bool>> ApproveAsync(long id, long reviewerUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync<bool>("Identity.ApproveRegistration", async () =>
        {
            var reg = await db.ShopRegistrationRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (reg is null)
                return Result<bool>.NotFound(Errors.Registration.NotFound(id));

            if (reg.Status != RegistrationStatus.Pending)
                return Result<bool>.Conflict(Errors.Registration.NotPending);

            var onboardResult = await onboardingService.OnboardFromApprovedRequestAsync(reg, ct);
            if (!onboardResult.IsSuccess)
                return Result<bool>.Failure(onboardResult.Errors.FirstOrDefault() ?? Errors.Registration.OnboardFailed);

            reg.Status           = RegistrationStatus.Approved;
            reg.ReviewedByUserId = reviewerUserId;
            reg.ReviewedAtUtc    = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> RejectAsync(long id, string reason, long reviewerUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync<bool>("Identity.RejectRegistration", async () =>
        {
            var reg = await db.ShopRegistrationRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (reg is null)
                return Result<bool>.NotFound(Errors.Registration.NotFound(id));

            if (reg.Status != RegistrationStatus.Pending)
                return Result<bool>.Conflict(Errors.Registration.NotPending);

            reg.Status           = RegistrationStatus.Rejected;
            reg.RejectionReason  = reason;
            reg.ReviewedByUserId = reviewerUserId;
            reg.ReviewedAtUtc    = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }
}
