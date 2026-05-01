using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Warranty.Entities;

[Auditable("Warranty.WarrantyClaim")]
public class WarrantyClaim : TenantEntity
{
    public long WarrantyRegistrationId { get; set; }
    public string ClaimNumber { get; set; } = default!;
    public DateTime ClaimDate { get; set; }
    public string IssueDescription { get; set; } = default!;
    public ClaimStatus Status { get; set; } = ClaimStatus.Open;
    public string? ResolutionNotes { get; set; }
    public decimal? RepairCost { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? AttachmentFileIds { get; set; }

    public WarrantyRegistration Registration { get; set; } = default!;
}
