using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hardware.Entities;

[Auditable("Hardware.DeviceProfile")]
public class DeviceProfile : TenantEntity
{
    /// <summary>Client-assigned unique identifier (e.g. machine hostname + UUID).</summary>
    public string DeviceId { get; set; } = "";

    public DeviceClass Class { get; set; }

    public string VendorCode { get; set; } = "";

    public string ModelCode { get; set; } = "";

    /// <summary>JSON: connection type (USB/Network/Bluetooth), IP, port, COM port, etc.</summary>
    public string ConnectionJson { get; set; } = "{}";

    /// <summary>Logical role: "ReceiptPrinter", "LabelPrinter", "CashDrawer1", etc.</summary>
    public string Role { get; set; } = "";

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastUsedAtUtc { get; set; }
}
