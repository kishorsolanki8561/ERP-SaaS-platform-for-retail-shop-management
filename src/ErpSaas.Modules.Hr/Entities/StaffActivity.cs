using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Hr.Entities;

public class StaffActivity : TenantEntity
{
    public long EmployeeId { get; set; }
    public string ActivityType { get; set; } = default!;
    public string? Description { get; set; }
    public long? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public Employee Employee { get; set; } = default!;
}
