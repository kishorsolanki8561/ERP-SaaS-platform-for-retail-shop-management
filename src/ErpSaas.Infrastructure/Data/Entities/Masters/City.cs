using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class City : BaseEntity
{
    public long StateId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public State State { get; set; } = null!;
}
