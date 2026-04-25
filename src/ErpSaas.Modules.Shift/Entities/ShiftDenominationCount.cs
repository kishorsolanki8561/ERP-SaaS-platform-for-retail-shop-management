using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Shift.Entities;

public class ShiftDenominationCount : TenantEntity
{
    public long ShiftId { get; set; }
    public ShiftDenominationPhase Phase { get; set; }
    public int Denomination { get; set; }
    public int Count { get; set; }
    public decimal Subtotal { get; set; }

    public Shift Shift { get; set; } = null!;
}
