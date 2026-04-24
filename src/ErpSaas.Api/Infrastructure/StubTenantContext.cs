using ErpSaas.Shared.Data;

namespace ErpSaas.Api.Infrastructure;

// Phase 0 stub — replaced by TenantContextMiddleware in Phase 1 (Identity module).
internal sealed class StubTenantContext : ITenantContext
{
    public long ShopId => 0;
    public long CurrentUserId => 0;
    public IReadOnlyList<string> CurrentUserRoles => [];
}
