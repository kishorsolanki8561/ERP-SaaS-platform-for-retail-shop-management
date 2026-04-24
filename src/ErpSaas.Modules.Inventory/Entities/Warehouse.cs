using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Inventory.Entities;

/// <summary>Physical or logical storage location within a shop.</summary>
public class Warehouse : TenantEntity
{
    /// <summary>Short code, unique per shop (e.g. "WH-MAIN").</summary>
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Whether this is the default warehouse for new transactions.</summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
