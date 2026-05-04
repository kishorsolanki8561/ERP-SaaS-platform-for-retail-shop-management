using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Crm.Entities;

[Auditable("Customer.Mutated")]
public class Customer : TenantEntity
{
    [AuditField("Customer Code")]
    public string CustomerCode { get; set; } = "";

    [AuditField("Display Name")]
    public string DisplayName { get; set; } = "";

    [AuditField("Customer Type")]
    public string CustomerType { get; set; } = "";

    [AuditField("Email")]
    public string? Email { get; set; }

    [AuditField("Phone")]
    public string? Phone { get; set; }

    [AuditField("GST Number")]
    public string? GstNumber { get; set; }

    [AuditField("Credit Limit")]
    public decimal CreditLimitAmount { get; set; } = 0m;

    [AuditField("Outstanding Amount")]
    public decimal OutstandingAmount { get; set; } = 0m;

    public long? CustomerGroupId { get; set; }

    [AuditField("Active")]
    public bool IsActive { get; set; } = true;

    public CustomerGroup? CustomerGroup { get; set; }
    public ICollection<CustomerAddress> Addresses { get; set; } = [];
}
