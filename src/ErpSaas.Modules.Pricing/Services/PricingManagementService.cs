using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Pricing.Entities;
using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Pricing.Services;

public sealed class PricingManagementService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    IPricingEngine engine,
    ILogger<PricingManagementService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IPricingManagementService
{
    public async Task<IReadOnlyList<DiscountRuleDto>> ListDiscountRulesAsync(CancellationToken ct = default)
    {
        return await db.Set<DiscountRule>()
            .Where(r => !r.IsDeleted)
            .Select(r => new DiscountRuleDto(r.Id, r.Name, r.DiscountTypeCode, r.Scope,
                r.PercentValue, r.FixedValue, r.StartDate, r.EndDate, r.Priority, r.IsActive))
            .ToListAsync(ct);
    }

    public async Task<Result<long>> CreateDiscountRuleAsync(CreateDiscountRuleDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Pricing.CreateDiscountRule", async () =>
        {
            var rule = new DiscountRule
            {
                ShopId = tenant.ShopId,
                Name = dto.Name,
                DiscountTypeCode = dto.DiscountTypeCode,
                Scope = dto.Scope,
                ProductId = dto.ProductId,
                CategoryId = dto.CategoryId,
                CustomerTypeId = dto.CustomerTypeId,
                PercentValue = dto.PercentValue,
                FixedValue = dto.FixedValue,
                BuyQty = dto.BuyQty,
                GetQty = dto.GetQty,
                MinInvoiceAmount = dto.MinInvoiceAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Priority = dto.Priority,
                IsStackable = dto.IsStackable,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<DiscountRule>().Add(rule);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(rule.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ToggleDiscountRuleAsync(long id, bool isActive, CancellationToken ct = default)
    {
        return await ExecuteAsync("Pricing.ToggleDiscountRule", async () =>
        {
            var rule = await db.Set<DiscountRule>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (rule is null) return Result<bool>.NotFound(Errors.Pricing.DiscountRuleNotFound);
            rule.IsActive = isActive;
            rule.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> CreateExtraChargeAsync(CreateExtraChargeDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Pricing.CreateExtraCharge", async () =>
        {
            var charge = new ExtraChargeRule
            {
                ShopId = tenant.ShopId,
                Name = dto.Name,
                Type = dto.Type,
                Value = dto.Value,
                IsTaxable = dto.IsTaxable,
                GstRate = dto.GstRate,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<ExtraChargeRule>().Add(charge);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(charge.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ToggleExtraChargeAsync(long id, bool isActive, CancellationToken ct = default)
    {
        return await ExecuteAsync("Pricing.ToggleExtraCharge", async () =>
        {
            var charge = await db.Set<ExtraChargeRule>().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (charge is null) return Result<bool>.NotFound(Errors.Pricing.ExtraChargeNotFound);
            charge.IsActive = isActive;
            charge.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> CreateOfferAsync(CreateOfferDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Pricing.CreateOffer", async () =>
        {
            var exists = await db.Set<Offer>().AnyAsync(o => o.Code == dto.Code, ct);
            if (exists) return Result<long>.Conflict(Errors.Pricing.OfferCodeExists);

            var offer = new Offer
            {
                ShopId = tenant.ShopId,
                Code = dto.Code,
                Name = dto.Name,
                Type = dto.Type,
                RulesJson = dto.RulesJson,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Offer>().Add(offer);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(offer.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ToggleOfferAsync(long id, bool isActive, CancellationToken ct = default)
    {
        return await ExecuteAsync("Pricing.ToggleOffer", async () =>
        {
            var offer = await db.Set<Offer>().FirstOrDefaultAsync(o => o.Id == id, ct);
            if (offer is null) return Result<bool>.NotFound(Errors.Pricing.OfferNotFound);
            offer.IsActive = isActive;
            offer.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<CartCalculationResult> CalculateAsync(CartInput cart, CancellationToken ct = default)
    {
        var rules = await ListDiscountRulesAsync(ct);
        var charges = await db.Set<ExtraChargeRule>()
            .Where(c => c.IsActive && !c.IsDeleted)
            .Select(c => new CreateExtraChargeDto(c.Name, c.Type, c.Value, c.IsTaxable, c.GstRate))
            .ToListAsync(ct);

        return engine.Calculate(cart, rules, charges);
    }
}
