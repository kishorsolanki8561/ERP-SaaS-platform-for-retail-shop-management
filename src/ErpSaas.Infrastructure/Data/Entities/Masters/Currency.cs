using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class Currency : BaseEntity
{
    public string Code { get; set; } = "";        // ISO 4217
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}
