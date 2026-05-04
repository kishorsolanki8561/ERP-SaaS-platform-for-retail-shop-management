using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Marketing;

public enum LeadSource { Website, Referral, Ad, Event }
public enum LeadStatus { New, Contacted, Qualified, Converted, Lost }

public class Lead : BaseEntity
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? BusinessName { get; set; }
    public string? Message { get; set; }
    public string? Notes { get; set; }
    public string CityCode { get; set; } = "";
    public string StateCode { get; set; } = "";
    public string VerticalCode { get; set; } = "";
    public int? ShopsCount { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public string? UtmSource { get; set; }
    public string? UtmCampaign { get; set; }
    public long? ConvertedShopId { get; set; }
    public long? AssignedUserId { get; set; }
    public DateTime? LastContactedAtUtc { get; set; }
}
