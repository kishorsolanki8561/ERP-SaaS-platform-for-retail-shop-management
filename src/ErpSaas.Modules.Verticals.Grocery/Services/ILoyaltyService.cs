using ErpSaas.Modules.Verticals.Grocery.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Verticals.Grocery.Services;

public record LoyaltyProgramDto(
    long Id,
    string Name,
    decimal PointsPerRupee,
    decimal RupeeValuePerPoint,
    decimal MinimumRedemptionPoints,
    decimal MaxRedemptionPercentPerBill,
    int PointExpiryDays,
    bool IsActive);

public record CustomerLoyaltyDto(
    long CustomerId,
    decimal TotalPoints,
    decimal RedeemablePoints,
    decimal PointsValue);

public record LoyaltyTransactionDto(
    long Id,
    long CustomerId,
    LoyaltyTransactionType TransactionType,
    decimal Points,
    decimal BalanceAfter,
    long? InvoiceId,
    string? Reference,
    DateTime CreatedAtUtc);

public record EarnPointsDto(long CustomerId, long InvoiceId, decimal InvoiceTotal);
public record RedeemPointsDto(long CustomerId, long InvoiceId, decimal PointsToRedeem);

public interface ILoyaltyService
{
    Task<LoyaltyProgramDto?> GetProgramAsync(CancellationToken ct = default);
    Task<Result<long>> CreateOrUpdateProgramAsync(LoyaltyProgramDto dto, CancellationToken ct = default);
    Task<CustomerLoyaltyDto> GetCustomerBalanceAsync(long customerId, CancellationToken ct = default);
    Task<Result<decimal>> EarnPointsAsync(EarnPointsDto dto, CancellationToken ct = default);
    Task<Result<decimal>> RedeemPointsAsync(RedeemPointsDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<LoyaltyTransactionDto>> GetCustomerHistoryAsync(long customerId, CancellationToken ct = default);
}
