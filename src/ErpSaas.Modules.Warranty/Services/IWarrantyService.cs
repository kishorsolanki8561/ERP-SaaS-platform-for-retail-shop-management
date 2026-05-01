using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Warranty.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record WarrantyRegistrationDto(
    long Id, string SerialNumber, string ProductName, string CustomerName,
    DateTime PurchaseDate, DateTime WarrantyEndDate, string StatusCode, WarrantyType Type);

public record RegisterWarrantyDto(
    long InvoiceId, long InvoiceLineId, long ProductId, long CustomerId,
    string SerialNumber, DateTime PurchaseDate, int WarrantyMonths,
    WarrantyType Type, string? Terms, long? BranchId);

public record WarrantyClaimDto(
    long Id, string ClaimNumber, long WarrantyRegistrationId,
    DateTime ClaimDate, string IssueDescription, ClaimStatus Status,
    string? ResolutionNotes, decimal? RepairCost, DateTime? ResolvedDate);

public record CreateClaimDto(
    long WarrantyRegistrationId, DateTime ClaimDate,
    string IssueDescription, string? AttachmentFileIds);

public record ResolveClaimDto(
    ClaimStatus Status, string? ResolutionNotes, decimal? RepairCost);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IWarrantyService
{
    Task<Result<long>> RegisterWarrantyAsync(RegisterWarrantyDto dto, CancellationToken ct = default);
    Task<WarrantyRegistrationDto?> GetBySerialAsync(string serial, CancellationToken ct = default);
    Task<IReadOnlyList<WarrantyRegistrationDto>> ListExpiringAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<WarrantyRegistrationDto>> ListByCustomerAsync(long customerId, CancellationToken ct = default);

    Task<Result<long>> CreateClaimAsync(CreateClaimDto dto, CancellationToken ct = default);
    Task<Result<bool>> ResolveClaimAsync(long claimId, ResolveClaimDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<WarrantyClaimDto>> ListClaimsAsync(CancellationToken ct = default);
}
