using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Masters;

public enum HsnSacType { HSN, SAC }

public class HsnSacCode : BaseEntity
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public HsnSacType Type { get; set; }
    public decimal? GstRate { get; set; }         // standard GST %
    public bool IsActive { get; set; } = true;
}
