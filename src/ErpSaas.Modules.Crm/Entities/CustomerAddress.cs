using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Crm.Entities;

public class CustomerAddress : TenantEntity
{
    public long CustomerId { get; set; }

    /// <summary>DDL key: ADDRESS_TYPE. Values: Billing | Shipping | Both.</summary>
    public string AddressType { get; set; } = "";

    public string Line1 { get; set; } = "";

    public string? Line2 { get; set; }

    public string? City { get; set; }

    public string? StateCode { get; set; }

    public string? PinCode { get; set; }

    public bool IsDefault { get; set; } = false;

    // Navigation
    public Customer Customer { get; set; } = null!;
}
