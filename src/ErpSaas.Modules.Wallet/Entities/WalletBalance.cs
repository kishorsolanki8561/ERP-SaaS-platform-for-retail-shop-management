using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Wallet.Entities;

[Auditable("Wallet.Balance")]
public class WalletBalance : TenantEntity
{
    public long CustomerId { get; set; }

    public string CustomerNameSnapshot { get; set; } = "";

    public decimal Balance { get; set; } = 0m;

    public DateTime? LastTransactionAtUtc { get; set; }
}
