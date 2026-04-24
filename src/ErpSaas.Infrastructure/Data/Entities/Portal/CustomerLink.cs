namespace ErpSaas.Infrastructure.Data.Entities.Portal;

/// <summary>
/// Links a PlatformCustomer to a specific Shop's customer record (by CustomerId in TenantDB).
/// </summary>
public sealed class CustomerLink
{
    public long Id { get; set; }
    public long PlatformCustomerId { get; set; }
    public long ShopId { get; set; }
    public long TenantCustomerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LinkedAtUtc { get; set; }

    public PlatformCustomer PlatformCustomer { get; set; } = null!;
}
