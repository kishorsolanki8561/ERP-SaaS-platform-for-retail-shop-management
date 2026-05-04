using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public sealed record ShopSummaryDto(
    long Id,
    string ShopCode,
    string LegalName,
    string? TradeName,
    bool IsActive,
    string? PlanLabel,
    int UserCount,
    DateTime? LastActivityAtUtc);

public sealed record ShopDetailDto(
    long Id,
    string ShopCode,
    string LegalName,
    string? TradeName,
    string? GstNumber,
    string? City,
    string? StateCode,
    bool IsActive,
    string? PlanLabel,
    string? PlanCode,
    DateTime? SubscriptionStartDate,
    DateTime? SubscriptionEndDate,
    int UserCount,
    long InvoiceCount,
    decimal RevenueCurrentMonth);

public sealed record ShopUserDto(
    long Id,
    string DisplayName,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime? LastLoginAtUtc);

// ── New Part 3 DTOs ────────────────────────────────────────────────────────────

public sealed record SubscriptionDashboardDto(
    decimal MRR,
    decimal ARR,
    int ActiveShops,
    int TrialShops,
    int ExpiredShops,
    decimal ChurnRate,
    IReadOnlyList<UpcomingRenewalDto> UpcomingRenewals);

public sealed record UpcomingRenewalDto(
    long ShopId,
    string ShopName,
    string PlanLabel,
    decimal Amount,
    DateTime RenewsAtUtc);

public sealed record SystemHealthDto(
    int ErrorsLast24h,
    int HangfireQueueDepth,
    bool DbPingOk,
    bool RedisPingOk,
    string ApiVersion);

public sealed record PlatformSubscriptionPlanDto(
    long Id,
    string Code,
    string Label,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int MaxUsers,
    int MaxProducts,
    int MaxInvoicesPerMonth,
    int StorageQuotaMb,
    int SmsQuotaPerMonth,
    int EmailQuotaPerMonth,
    bool IsActive,
    IReadOnlyList<string> Features);

public sealed record CreatePlanDto(
    string Code,
    string Label,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int MaxUsers,
    int MaxProducts,
    int MaxInvoicesPerMonth,
    int StorageQuotaMb,
    int SmsQuotaPerMonth,
    int EmailQuotaPerMonth,
    IReadOnlyList<string> Features);

public sealed record UpdatePlanDto(
    string? Label,
    decimal? MonthlyPrice,
    decimal? AnnualPrice,
    int? MaxUsers,
    int? MaxProducts,
    int? MaxInvoicesPerMonth,
    int? StorageQuotaMb,
    int? SmsQuotaPerMonth,
    int? EmailQuotaPerMonth,
    bool? IsActive,
    IReadOnlyList<string>? Features);

public sealed record ToggleFeatureDto(string FeatureCode, bool Enabled);
public sealed record SuspendShopDto(string Reason);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IPlatformAdminService
{
    Task<(IReadOnlyList<ShopSummaryDto> Items, int TotalCount)> ListShopsAsync(
        int pageNumber, int pageSize, string? search, CancellationToken ct = default);

    Task<ShopDetailDto?> GetShopDetailAsync(long shopId, CancellationToken ct = default);

    Task<IReadOnlyList<ShopUserDto>> ListShopUsersAsync(long shopId, CancellationToken ct = default);

    Task<SubscriptionDashboardDto>  GetSubscriptionDashboardAsync(CancellationToken ct = default);
    Task<SystemHealthDto>           GetSystemHealthAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PlatformSubscriptionPlanDto>> ListPlansAsync(CancellationToken ct = default);
    Task<Result<long>>              CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<Result<bool>>              UpdatePlanAsync(long planId, UpdatePlanDto dto, CancellationToken ct = default);

    Task<Result<bool>>              ToggleShopFeatureAsync(long shopId, ToggleFeatureDto dto, CancellationToken ct = default);
    Task<Result<bool>>              SuspendShopAsync(long shopId, SuspendShopDto dto, CancellationToken ct = default);
    Task<Result<bool>>              ActivateShopAsync(long shopId, CancellationToken ct = default);
}
