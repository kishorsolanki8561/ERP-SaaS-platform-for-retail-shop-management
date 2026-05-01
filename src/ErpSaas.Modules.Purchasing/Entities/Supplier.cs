using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.Supplier")]
public class Supplier : TenantEntity
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? GstNumber { get; set; }
    public string? PanNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
