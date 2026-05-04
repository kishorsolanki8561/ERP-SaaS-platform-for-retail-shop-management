using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hardware.Entities;

[Auditable("Hardware.LabelTemplate")]
public class LabelTemplate : TenantEntity
{
    public string Name { get; set; } = "";

    public LabelType LabelType { get; set; }

    public int PageWidthMm { get; set; } = 40;

    public int PageHeightMm { get; set; } = 25;

    /// <summary>ZPL body with {{productName}}, {{barcode}}, {{price}}, {{date}} placeholders.</summary>
    public string ZplTemplate { get; set; } = "";

    /// <summary>Optional TSPL alternative for TSC / Godex printers.</summary>
    public string? TsplTemplate { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
