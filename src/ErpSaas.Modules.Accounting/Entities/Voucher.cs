using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.Voucher")]
public class Voucher : TenantEntity
{
    [AuditField("Voucher Number")]
    public string VoucherNumber { get; set; } = default!;

    [AuditField("Voucher Date")]
    public DateTime VoucherDate { get; set; }

    [AuditField("Type")]
    public VoucherType VoucherType { get; set; }

    [AuditField("Status")]
    public VoucherStatus Status { get; set; } = VoucherStatus.Draft;

    [AuditField("Narration")]
    public string? Narration { get; set; }

    [AuditField("Total Debit")]
    public decimal TotalDebit { get; set; }

    [AuditField("Total Credit")]
    public decimal TotalCredit { get; set; }

    [AuditField("Source Document Type")]
    public string? SourceDocumentType { get; set; }

    public long? SourceDocumentId { get; set; }

    [AuditField("Posted")]
    public bool IsPosted { get; set; }

    [AuditField("Posted At")]
    public DateTime? PostedAtUtc { get; set; }

    public long? FinancialYearId { get; set; }
    public long? ReversedByVoucherId { get; set; }

    public FinancialYear? FinancialYear { get; set; }
    public ICollection<VoucherEntry> Entries { get; set; } = [];
}
