using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public record SubmitRegistrationRequest(
    string ShopCode,
    string LegalName,
    string AdminEmail,
    string AdminDisplayName,
    string Password,
    string? TradeName = null,
    string? GstNumber = null,
    string? ContactPhone = null,
    string? Notes = null);

public record RegistrationSummaryDto(
    long Id,
    string ShopCode,
    string LegalName,
    string AdminEmail,
    string? ContactPhone,
    RegistrationStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? RejectionReason);

public interface IShopRegistrationService
{
    Task<Result<long>> SubmitAsync(SubmitRegistrationRequest request, CancellationToken ct = default);

    Task<(IReadOnlyList<RegistrationSummaryDto> Items, int TotalCount)> ListAsync(
        int pageNumber, int pageSize, RegistrationStatus? status, CancellationToken ct = default);

    Task<ShopRegistrationRequest?> GetAsync(long id, CancellationToken ct = default);

    Task<Result<bool>> ApproveAsync(long id, long reviewerUserId, CancellationToken ct = default);

    Task<Result<bool>> RejectAsync(long id, string reason, long reviewerUserId, CancellationToken ct = default);
}
