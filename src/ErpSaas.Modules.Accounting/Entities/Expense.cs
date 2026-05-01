using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.Expense")]
public class Expense : TenantEntity
{
    public DateTime ExpenseDate { get; set; }
    public long AccountId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string PaymentModeCode { get; set; } = default!;
    public long? PaidFromAccountId { get; set; }
    public string? AttachmentFileId { get; set; }
    public long? VoucherId { get; set; }
    public bool IsRecurring { get; set; }
    public ExpenseRecurrenceInterval? RecurrenceInterval { get; set; }

    public Account Account { get; set; } = default!;
    public Voucher? Voucher { get; set; }
}
