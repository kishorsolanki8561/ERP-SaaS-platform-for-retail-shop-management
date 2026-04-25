using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Shift.Entities;

[Auditable("Shift")]
public class Shift : TenantEntity
{
    public long BranchId { get; set; }
    public long CashierUserId { get; set; }
    public string CashierNameSnapshot { get; set; } = "";
    public string? CashierPhoneSnapshot { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public decimal? ClosingCashCounted { get; set; }
    public decimal? SystemComputedCash { get; set; }
    public decimal? CashVariance { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public string? ClosingNotes { get; set; }
    public long? ForcedClosedByUserId { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCardSales { get; set; }
    public decimal TotalUpiSales { get; set; }
    public decimal TotalWalletDebits { get; set; }
    public decimal TotalCashRefunds { get; set; }

    public ICollection<ShiftCashMovement> CashMovements { get; set; } = [];
    public ICollection<ShiftDenominationCount> DenominationCounts { get; set; } = [];
}
