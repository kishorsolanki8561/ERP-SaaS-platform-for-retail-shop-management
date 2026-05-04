using ErpSaas.Modules.ServiceJobs.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.ServiceJobs.Services;

public record ReceiveServiceJobDto(
    long CustomerId,
    long BranchId,
    long? ProductId,
    string ItemDescription,
    string? SerialNumber,
    string ReportedIssue,
    bool IsUnderWarranty,
    long? WarrantyRegistrationId);

public record DiagnoseServiceJobDto(
    string DiagnosisNotes,
    decimal? EstimatedCost,
    long? AssignedTechnicianUserId);

public record AddPartDto(long ProductId, decimal Quantity);
public record AddLaborDto(long TechnicianUserId, decimal Hours, decimal HourlyRate, string? Notes);

public record ServiceJobSummaryDto(
    long Id,
    string JobNumber,
    DateTime ReceivedAtDate,
    long CustomerId,
    string? CustomerNameSnapshot,
    string ItemDescription,
    string? SerialNumber,
    ServiceJobStatus Status,
    decimal TotalCost,
    DateTime? DeliveredAtUtc);

public record ServiceJobDetailDto(
    long Id,
    string JobNumber,
    DateTime ReceivedAtDate,
    long CustomerId,
    string? CustomerNameSnapshot,
    string? CustomerPhoneSnapshot,
    long? ProductId,
    string ItemDescription,
    string? SerialNumber,
    bool IsUnderWarranty,
    long? WarrantyRegistrationId,
    string ReportedIssue,
    string? DiagnosisNotes,
    ServiceJobStatus Status,
    long? AssignedTechnicianUserId,
    decimal? EstimatedCost,
    decimal ActualPartsCost,
    decimal ActualLaborCost,
    decimal TotalCost,
    DateTime? DiagnosedAtUtc,
    DateTime? ApprovedByCustomerAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? DeliveredAtUtc,
    long? ResultingInvoiceId,
    IReadOnlyList<PartLineDto> Parts,
    IReadOnlyList<LaborLineDto> LaborEntries);

public record PartLineDto(long Id, long ProductId, string ProductNameSnapshot, decimal Quantity, decimal UnitCost, decimal LineCost);
public record LaborLineDto(long Id, long TechnicianUserId, string TechnicianNameSnapshot, decimal Hours, decimal HourlyRate, decimal LaborCost, string? Notes);

public interface IServiceJobService
{
    Task<Result<long>> ReceiveAsync(ReceiveServiceJobDto dto, CancellationToken ct = default);
    Task<Result<bool>> DiagnoseAsync(long jobId, DiagnoseServiceJobDto dto, CancellationToken ct = default);
    Task<Result<bool>> CustomerApproveAsync(long jobId, CancellationToken ct = default);
    Task<Result<bool>> StartProgressAsync(long jobId, CancellationToken ct = default);
    Task<Result<bool>> MarkReadyAsync(long jobId, CancellationToken ct = default);
    Task<Result<bool>> DeliverAsync(long jobId, CancellationToken ct = default);
    Task<Result<bool>> RejectAsync(long jobId, string reason, CancellationToken ct = default);
    Task<Result<bool>> AddPartAsync(long jobId, AddPartDto dto, CancellationToken ct = default);
    Task<Result<bool>> AddLaborAsync(long jobId, AddLaborDto dto, CancellationToken ct = default);
    Task<ServiceJobDetailDto?> GetAsync(long jobId, CancellationToken ct = default);
    Task<ServiceJobDetailDto?> GetByJobNumberAsync(string jobNumber, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceJobSummaryDto>> ListAsync(ServiceJobStatus? status, CancellationToken ct = default);
}
