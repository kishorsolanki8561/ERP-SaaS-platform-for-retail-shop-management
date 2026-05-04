using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.ServiceJobs.Entities;

public class ServiceJobLabor : TenantEntity
{
    public long ServiceJobId { get; set; }
    public long TechnicianUserId { get; set; }
    public string TechnicianNameSnapshot { get; set; } = default!;
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal LaborCost { get; set; }
    public string? Notes { get; set; }

    public ServiceJob Job { get; set; } = default!;
}
