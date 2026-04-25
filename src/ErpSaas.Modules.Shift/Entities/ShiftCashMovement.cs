using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Shift.Entities;

public class ShiftCashMovement : TenantEntity
{
    public long ShiftId { get; set; }
    public DateTime MovementAtUtc { get; set; }
    public ShiftCashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? ReasonCode { get; set; }
    public string? Notes { get; set; }
    public long? AuthorizedByUserId { get; set; }
    public long? RelatedExpenseId { get; set; }

    public Shift Shift { get; set; } = null!;
}
