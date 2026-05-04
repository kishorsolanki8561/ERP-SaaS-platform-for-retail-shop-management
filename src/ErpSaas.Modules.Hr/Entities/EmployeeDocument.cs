using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Hr.Entities;

public class EmployeeDocument : TenantEntity
{
    public long EmployeeId { get; set; }
    public string DocumentType { get; set; } = default!;
    public long UploadedFileId { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public Employee Employee { get; set; } = default!;
}
