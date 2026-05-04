using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Verticals.Grocery.Entities;
using ErpSaas.Modules.Verticals.Grocery.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Verticals.Grocery.Services;

public sealed class LoyaltyService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), ILoyaltyService
{
    public async Task<LoyaltyProgramDto?> GetProgramAsync(CancellationToken ct = default)
    {
        var p = await _db.Set<LoyaltyProgram>().FirstOrDefaultAsync(ct);
        return p is null ? null : Map(p);
    }

    public async Task<Result<long>> CreateOrUpdateProgramAsync(LoyaltyProgramDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Loyalty.CreateOrUpdate", async () =>
        {
            var existing = await _db.Set<LoyaltyProgram>().FirstOrDefaultAsync(ct);
            if (existing is null)
            {
                existing = new LoyaltyProgram
                {
                    ShopId = tenant.ShopId,
                    Name = dto.Name,
                    PointsPerRupee = dto.PointsPerRupee,
                    RupeeValuePerPoint = dto.RupeeValuePerPoint,
                    MinimumRedemptionPoints = dto.MinimumRedemptionPoints,
                    MaxRedemptionPercentPerBill = dto.MaxRedemptionPercentPerBill,
                    PointExpiryDays = dto.PointExpiryDays,
                    IsActive = dto.IsActive,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                _db.Set<LoyaltyProgram>().Add(existing);
            }
            else
            {
                existing.Name = dto.Name;
                existing.PointsPerRupee = dto.PointsPerRupee;
                existing.RupeeValuePerPoint = dto.RupeeValuePerPoint;
                existing.MinimumRedemptionPoints = dto.MinimumRedemptionPoints;
                existing.MaxRedemptionPercentPerBill = dto.MaxRedemptionPercentPerBill;
                existing.PointExpiryDays = dto.PointExpiryDays;
                existing.IsActive = dto.IsActive;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(existing.Id);
        }, ct, useTransaction: true);
    }

    public async Task<CustomerLoyaltyDto> GetCustomerBalanceAsync(long customerId, CancellationToken ct = default)
    {
        var program = await _db.Set<LoyaltyProgram>().FirstOrDefaultAsync(ct);
        if (program is null) return new CustomerLoyaltyDto(customerId, 0, 0, 0);

        var transactions = await _db.Set<LoyaltyTransaction>()
            .Where(t => t.CustomerId == customerId)
            .ToListAsync(ct);

        var balance = transactions.Sum(t =>
            t.TransactionType is LoyaltyTransactionType.Earn or LoyaltyTransactionType.Bonus
                ? t.Points : -t.Points);

        var redeemable = Math.Max(0, balance - program.MinimumRedemptionPoints);
        return new CustomerLoyaltyDto(customerId, balance, redeemable, redeemable * program.RupeeValuePerPoint);
    }

    public async Task<Result<decimal>> EarnPointsAsync(EarnPointsDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Loyalty.Earn", async () =>
        {
            var program = await _db.Set<LoyaltyProgram>().FirstOrDefaultAsync(ct);
            if (program is null || !program.IsActive)
                return Result<decimal>.Success(0);

            var pointsEarned = Math.Round(dto.InvoiceTotal * program.PointsPerRupee, 2);
            var balance = await GetRawBalance(dto.CustomerId, ct);
            var newBalance = balance + pointsEarned;

            _db.Set<LoyaltyTransaction>().Add(new LoyaltyTransaction
            {
                ShopId = tenant.ShopId,
                CustomerId = dto.CustomerId,
                LoyaltyProgramId = program.Id,
                TransactionType = LoyaltyTransactionType.Earn,
                Points = pointsEarned,
                BalanceAfter = newBalance,
                InvoiceId = dto.InvoiceId,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(program.PointExpiryDays),
                CreatedAtUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
            return Result<decimal>.Success(pointsEarned);
        }, ct, useTransaction: true);
    }

    public async Task<Result<decimal>> RedeemPointsAsync(RedeemPointsDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Loyalty.Redeem", async () =>
        {
            var program = await _db.Set<LoyaltyProgram>().FirstOrDefaultAsync(ct);
            if (program is null || !program.IsActive)
                return Result<decimal>.Conflict(Errors.Grocery.ProgramNotFound);

            var balance = await GetRawBalance(dto.CustomerId, ct);
            if (balance < dto.PointsToRedeem)
                return Result<decimal>.Conflict(Errors.Grocery.InsufficientPoints);

            var redeemValue = dto.PointsToRedeem * program.RupeeValuePerPoint;
            var newBalance = balance - dto.PointsToRedeem;

            _db.Set<LoyaltyTransaction>().Add(new LoyaltyTransaction
            {
                ShopId = tenant.ShopId,
                CustomerId = dto.CustomerId,
                LoyaltyProgramId = program.Id,
                TransactionType = LoyaltyTransactionType.Redeem,
                Points = dto.PointsToRedeem,
                BalanceAfter = newBalance,
                InvoiceId = dto.InvoiceId,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
            return Result<decimal>.Success(redeemValue);
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<LoyaltyTransactionDto>> GetCustomerHistoryAsync(long customerId, CancellationToken ct = default)
    {
        return await _db.Set<LoyaltyTransaction>()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new LoyaltyTransactionDto(
                t.Id, t.CustomerId, t.TransactionType, t.Points,
                t.BalanceAfter, t.InvoiceId, t.Reference, t.CreatedAtUtc))
            .ToListAsync(ct);
    }

    private async Task<decimal> GetRawBalance(long customerId, CancellationToken ct)
    {
        var txns = await _db.Set<LoyaltyTransaction>()
            .Where(t => t.CustomerId == customerId)
            .ToListAsync(ct);
        return txns.Sum(t =>
            t.TransactionType is LoyaltyTransactionType.Earn or LoyaltyTransactionType.Bonus
                ? t.Points : -t.Points);
    }

    private static LoyaltyProgramDto Map(LoyaltyProgram p) => new(
        p.Id, p.Name, p.PointsPerRupee, p.RupeeValuePerPoint,
        p.MinimumRedemptionPoints, p.MaxRedemptionPercentPerBill,
        p.PointExpiryDays, p.IsActive);
}
