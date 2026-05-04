using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Entities;

[Auditable("Purchasing.Supplier")]
public class Supplier : TenantEntity
{
    [AuditField("Name")]
    public string Name { get; set; } = default!;

    [AuditField("Code")]
    public string? Code { get; set; }

    [AuditField("GST Number")]
    public string? GstNumber { get; set; }

    [AuditField("PAN Number")]
    public string? PanNumber { get; set; }

    [AuditField("Phone")]
    public string? Phone { get; set; }

    [AuditField("Email")]
    public string? Email { get; set; }

    [AuditField("Address")]
    public string? Address { get; set; }

    [AuditField("City")]
    public string? City { get; set; }

    [AuditField("State")]
    public string? State { get; set; }

    [AuditField("Pincode")]
    public string? Pincode { get; set; }

    [AuditField("Opening Balance")]
    public decimal OpeningBalance { get; set; }

    [AuditField("Active")]
    public bool IsActive { get; set; } = true;

    [AuditField("Notes")]
    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
