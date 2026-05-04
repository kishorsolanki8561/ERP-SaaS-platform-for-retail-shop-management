using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hardware.Entities;

[Auditable("Hardware.ReceiptTemplate")]
public class ReceiptTemplate : TenantEntity
{
    public string Name { get; set; } = "";

    /// <summary>"Retail80mm" | "GiftReceipt" | "ReturnSlip" | "A4Invoice"</summary>
    public string TemplateType { get; set; } = "Retail80mm";

    /// <summary>JSON config: shop name, address, GSTIN, footer text, QR code, etc.</summary>
    public string HeaderJson { get; set; } = "{}";

    public string FooterJson { get; set; } = "{}";

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
