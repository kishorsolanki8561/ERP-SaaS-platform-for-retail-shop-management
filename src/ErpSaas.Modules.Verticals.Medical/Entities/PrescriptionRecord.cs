using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Verticals.Medical.Entities;

public class PrescriptionRecord : TenantEntity
{
    public long DrugBatchId { get; set; }
    public long InvoiceId { get; set; }
    public long CustomerId { get; set; }
    public string DoctorName { get; set; } = default!;
    public string? DoctorRegistrationNumber { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public decimal QuantityDispensed { get; set; }
    public string? FileId { get; set; }
    public string? Notes { get; set; }

    public DrugBatch Batch { get; set; } = default!;
}
