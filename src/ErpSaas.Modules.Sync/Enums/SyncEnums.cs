namespace ErpSaas.Modules.Sync.Enums;

public enum DeviceType
{
    DesktopPos,
    MobilePos,
    TabletPos,
    WebBrowser,
}

public enum OfflineCommandStatus
{
    Received,
    Applied,
    Rejected,
    AppliedWithWarning,
}

public enum InvoiceNumberAllocationStatus
{
    Active,
    Exhausted,
    Released,
}
