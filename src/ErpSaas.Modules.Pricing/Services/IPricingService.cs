using ErpSaas.Modules.Pricing.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Pricing.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CartLineInput(long ProductId, long? CategoryId, decimal Quantity, decimal UnitPrice);

public record CartInput(
    long? CustomerId, long? CustomerTypeId,
    DateTime Date,
    IReadOnlyList<CartLineInput> Lines);

public record AppliedDiscount(string RuleName, string DiscountTypeCode, decimal Amount);

public record CartLineResult(
    long ProductId, decimal Quantity, decimal UnitPrice,
    decimal DiscountAmount, decimal TaxableAmount,
    decimal GstRate, decimal CgstAmount, decimal SgstAmount,
    decimal LineTotal, IReadOnlyList<AppliedDiscount> AppliedDiscounts);

public record ExtraChargeResult(string Name, decimal Amount, bool IsTaxable);

public record CartCalculationResult(
    IReadOnlyList<CartLineResult> Lines,
    decimal SubTotal, decimal TotalDiscount,
    decimal TotalTaxableAmount, decimal TotalTax,
    IReadOnlyList<ExtraChargeResult> ExtraCharges, decimal TotalExtraCharges,
    decimal GrandTotal);

public record DiscountRuleDto(
    long Id, string Name, string DiscountTypeCode, DiscountScope Scope,
    decimal? PercentValue, decimal? FixedValue,
    DateTime StartDate, DateTime EndDate, int Priority, bool IsActive);

public record CreateDiscountRuleDto(
    string Name, string DiscountTypeCode, DiscountScope Scope,
    long? ProductId, long? CategoryId, long? CustomerTypeId,
    decimal? PercentValue, decimal? FixedValue,
    int? BuyQty, int? GetQty, decimal? MinInvoiceAmount,
    DateTime StartDate, DateTime EndDate, int Priority, bool IsStackable);

public record CreateExtraChargeDto(string Name, ChargeType Type, decimal Value, bool IsTaxable, decimal? GstRate);

public record CreateOfferDto(string Code, string Name, OfferType Type, string? RulesJson, DateTime StartDate, DateTime EndDate);

// ── Interfaces ────────────────────────────────────────────────────────────────

public interface IPricingEngine
{
    CartCalculationResult Calculate(CartInput cart, IReadOnlyList<DiscountRuleDto> rules, IReadOnlyList<CreateExtraChargeDto> charges);
}

public interface IPricingManagementService
{
    // Discount rules
    Task<IReadOnlyList<DiscountRuleDto>> ListDiscountRulesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateDiscountRuleAsync(CreateDiscountRuleDto dto, CancellationToken ct = default);
    Task<Result<bool>> ToggleDiscountRuleAsync(long id, bool isActive, CancellationToken ct = default);

    // Extra charges
    Task<Result<long>> CreateExtraChargeAsync(CreateExtraChargeDto dto, CancellationToken ct = default);
    Task<Result<bool>> ToggleExtraChargeAsync(long id, bool isActive, CancellationToken ct = default);

    // Offers
    Task<Result<long>> CreateOfferAsync(CreateOfferDto dto, CancellationToken ct = default);
    Task<Result<bool>> ToggleOfferAsync(long id, bool isActive, CancellationToken ct = default);

    // Calculation endpoint (loads rules + calls engine)
    Task<CartCalculationResult> CalculateAsync(CartInput cart, CancellationToken ct = default);
}
