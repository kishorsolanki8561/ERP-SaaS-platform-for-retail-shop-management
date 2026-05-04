using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Verticals;

public class VerticalPack : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string SeedManifestJson { get; set; } = "{}";
    public string FeatureFlagsCsv { get; set; } = string.Empty;
    public string? DefaultInvoiceTemplateCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
}
