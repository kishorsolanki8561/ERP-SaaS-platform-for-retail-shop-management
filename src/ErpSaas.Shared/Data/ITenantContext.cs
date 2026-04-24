namespace ErpSaas.Shared.Data;

public interface ITenantContext
{
    long ShopId { get; }
    long CurrentUserId { get; }
    IReadOnlyList<string> CurrentUserRoles { get; }
}
