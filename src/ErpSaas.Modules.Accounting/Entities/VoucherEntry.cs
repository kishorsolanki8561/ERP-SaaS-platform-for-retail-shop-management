using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.Accounting.Entities;

public class VoucherEntry : TenantEntity
{
    public long VoucherId { get; set; }
    public long AccountId { get; set; }
    public DebitCredit Type { get; set; }
    public decimal Amount { get; set; }
    public string? Narration { get; set; }

    public Voucher Voucher { get; set; } = default!;
    public Account Account { get; set; } = default!;
}
