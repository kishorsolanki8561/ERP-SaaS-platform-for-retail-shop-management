using ErpSaas.Modules.ServiceJobs.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.ServiceJobs.Entities;

public class ServiceJob : TenantEntity
{
    public string JobNumber { get; set; } = default!;
    public DateTime ReceivedAtDate { get; set; }
    public long BranchId { get; set; }
    public long CustomerId { get; set; }
    public long? ProductId { get; set; }
    public string ItemDescription { get; set; } = default!;
    public string? SerialNumber { get; set; }
    public long? WarrantyRegistrationId { get; set; }
    public bool IsUnderWarranty { get; set; }
    public string ReportedIssue { get; set; } = default!;
    public string? DiagnosisNotes { get; set; }
    public ServiceJobStatus Status { get; set; } = ServiceJobStatus.Received;
    public long? AssignedTechnicianUserId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal ActualPartsCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime? DiagnosedAtUtc { get; set; }
    public DateTime? ApprovedByCustomerAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public long? ResultingInvoiceId { get; set; }
    public long? ResultingWarrantyClaimId { get; set; }
    public string? CustomerNameSnapshot { get; set; }
    public string? CustomerPhoneSnapshot { get; set; }

    public ICollection<ServiceJobPart> Parts { get; set; } = [];
    public ICollection<ServiceJobLabor> LaborEntries { get; set; } = [];
}
