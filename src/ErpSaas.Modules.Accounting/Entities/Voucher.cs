using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.Voucher")]
public class Voucher : TenantEntity
{
    public string VoucherNumber { get; set; } = default!;
    public DateTime VoucherDate { get; set; }
    public VoucherType VoucherType { get; set; }
    public VoucherStatus Status { get; set; } = VoucherStatus.Draft;
    public string? Narration { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string? SourceDocumentType { get; set; }
    public long? SourceDocumentId { get; set; }
    public bool IsPosted { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public long? FinancialYearId { get; set; }
    public long? ReversedByVoucherId { get; set; }

    public FinancialYear? FinancialYear { get; set; }
    public ICollection<VoucherEntry> Entries { get; set; } = [];
}
