using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class Branch : BaseEntity
{
    public long ShopId { get; set; }
    public string Name { get; set; } = "";
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateCode { get; set; }
    public string? PinCode { get; set; }
    public string? Phone { get; set; }
    public string? GstNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHeadOffice { get; set; }

    public Shop Shop { get; set; } = null!;
}
