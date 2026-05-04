using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Marketing;

public class MarketingContent : BaseEntity
{
    public string Key { get; set; } = "";
    public string Locale { get; set; } = "en";
    public string? Title { get; set; }
    public string Body { get; set; } = "";
}
