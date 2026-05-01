using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Accounting.Entities;

[Auditable("Accounting.AccountGroup")]
public class AccountGroup : TenantEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public long? ParentId { get; set; }
    public AccountNature Nature { get; set; }
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }

    public AccountGroup? Parent { get; set; }
    public ICollection<AccountGroup> Children { get; set; } = [];
    public ICollection<Account> Accounts { get; set; } = [];
}
