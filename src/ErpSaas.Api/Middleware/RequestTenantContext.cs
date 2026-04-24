using ErpSaas.Shared.Data;

namespace ErpSaas.Api.Middleware;

public sealed class RequestTenantContext : ITenantContext
{
    public long ShopId { get; set; }
    public long CurrentUserId { get; set; }
    public IReadOnlyList<string> CurrentUserRoles { get; set; } = [];
}
