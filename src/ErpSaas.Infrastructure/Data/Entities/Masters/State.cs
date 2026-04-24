using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public class State : BaseEntity
{
    public long CountryId { get; set; }
    public string Code { get; set; } = "";        // e.g. MH, GJ
    public string Name { get; set; } = "";
    public string? GstStateCode { get; set; }     // 2-digit GST code
    public bool IsActive { get; set; } = true;

    public Country Country { get; set; } = null!;
    public ICollection<City> Cities { get; set; } = [];
}
