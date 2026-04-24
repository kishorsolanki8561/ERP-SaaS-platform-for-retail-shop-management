namespace ErpSaas.Infrastructure.MultiTenant;

public interface IShopConnectionResolver
{
    /// <summary>
    /// Returns the TenantDB connection string for the given shopId.
    /// Standard-tier shops share the default TenantDb; premium shops have their own.
    /// </summary>
    Task<string> ResolveAsync(long shopId, CancellationToken ct = default);
}
