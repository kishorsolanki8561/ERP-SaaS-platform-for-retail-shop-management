using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Crm.Entities;

public class CustomerGroup : TenantEntity
{
    /// <summary>Unique code per shop (e.g. "WHOLESALE", "GOVT").</summary>
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal DiscountPercent { get; set; } = 0m;

    public bool IsActive { get; set; } = true;

    public ICollection<Customer> Customers { get; set; } = [];
}
