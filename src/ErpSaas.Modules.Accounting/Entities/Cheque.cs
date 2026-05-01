using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.Cheque")]
public class Cheque : TenantEntity
{
    public ChequeDirection Direction { get; set; }
    public string ChequeNumber { get; set; } = default!;
    public DateTime ChequeDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public decimal Amount { get; set; }
    public long BankAccountId { get; set; }
    public string DrawerName { get; set; } = default!;
    public string DrawerBankName { get; set; } = default!;
    public ChequeStatus Status { get; set; } = ChequeStatus.Received;
    public DateTime? DepositedDate { get; set; }
    public DateTime? ClearedDate { get; set; }
    public DateTime? BouncedDate { get; set; }
    public string? BounceReasonCode { get; set; }
    public long? VoucherIdOnReceive { get; set; }
    public long? VoucherIdOnClear { get; set; }
    public long? VoucherIdOnBounce { get; set; }
    public long? RelatedInvoiceId { get; set; }
    public long? RelatedSupplierBillId { get; set; }

    public BankAccount BankAccount { get; set; } = default!;
}
