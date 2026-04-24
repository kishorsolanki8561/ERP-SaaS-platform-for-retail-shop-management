using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class Country : BaseEntity
{
    public string Code { get; set; } = "";        // ISO 3166-1 alpha-2
    public string Name { get; set; } = "";
    public string? PhoneCode { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<State> States { get; set; } = [];
}
