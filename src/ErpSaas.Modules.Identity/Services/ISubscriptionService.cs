using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record SubscriptionPlanDto(
    long   Id,
    string Code,
    string Label,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int    MaxUsers,
    int    MaxProducts,
    int    MaxInvoicesPerMonth,
    int    StorageQuotaMb,
    IReadOnlyList<string> Features);

public record CurrentSubscriptionDto(
    long   SubscriptionId,
    string PlanCode,
    string PlanLabel,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string BillingCycle,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    bool   IsActive,
    int    MaxUsers,
    int    MaxProducts,
    int    MaxInvoicesPerMonth,
    IReadOnlyList<string> Features);

public record ChangePlanDto(
    string PlanCode,
    string BillingCycle);   // "Monthly" | "Annual"

// ── Interface ────────────────────────────────────────────────────────────────

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> ListPlansAsync(CancellationToken ct = default);
    Task<CurrentSubscriptionDto?>           GetCurrentAsync(CancellationToken ct = default);
    Task<Result<bool>>                      ChangePlanAsync(ChangePlanDto dto, CancellationToken ct = default);
    Task<Result<bool>>                      CancelAsync(CancellationToken ct = default);
}
