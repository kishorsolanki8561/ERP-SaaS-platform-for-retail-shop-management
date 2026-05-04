using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

[Auditable("Masters.City")]
public class City : BaseEntity
{
    public long StateId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public State State { get; set; } = null!;
}
