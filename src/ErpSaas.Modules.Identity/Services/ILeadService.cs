using ErpSaas.Infrastructure.Data.Entities.Marketing;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record SubmitLeadDto(
    string Name,
    string Email,
    string Phone,
    string? BusinessName,
    string? Message,
    string CityCode,
    string StateCode,
    string VerticalCode,
    int? ShopsCount,
    string Source,
    string? UtmSource,
    string? UtmCampaign);

public sealed record LeadSummaryDto(
    long Id,
    string Name,
    string Email,
    string Phone,
    string? BusinessName,
    string VerticalCode,
    string Source,
    string Status,
    long? AssignedUserId,
    string? AssignedUserName,
    DateTime? LastContactedAtUtc,
    DateTime CreatedAtUtc);

public sealed record LeadDetailDto(
    long Id,
    string Name,
    string Email,
    string Phone,
    string? BusinessName,
    string? Message,
    string? Notes,
    string CityCode,
    string StateCode,
    string VerticalCode,
    int? ShopsCount,
    string Source,
    string Status,
    string? UtmSource,
    string? UtmCampaign,
    long? ConvertedShopId,
    long? AssignedUserId,
    DateTime? LastContactedAtUtc,
    DateTime CreatedAtUtc);

public sealed record UpdateLeadStatusDto(
    string Status,
    string? Notes);

// ── Interface ────────────────────────────────────────────────────────────────

public interface ILeadService
{
    Task<Result<long>>                              SubmitAsync(SubmitLeadDto dto, CancellationToken ct = default);
    Task<(IReadOnlyList<LeadSummaryDto> Items, int TotalCount)> ListAsync(int page, int pageSize, LeadStatus? status, CancellationToken ct = default);
    Task<LeadDetailDto?>                            GetAsync(long id, CancellationToken ct = default);
    Task<Result<bool>>                              AssignAsync(long leadId, long userId, CancellationToken ct = default);
    Task<Result<bool>>                              UpdateStatusAsync(long leadId, UpdateLeadStatusDto dto, CancellationToken ct = default);
    Task<Result<long>>                              ConvertAsync(long leadId, CancellationToken ct = default);
}
