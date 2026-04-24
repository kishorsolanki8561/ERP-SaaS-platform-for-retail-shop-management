namespace ErpSaas.Modules.Identity.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(long userId, long shopId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFeatureCodesAsync(long shopId, CancellationToken ct = default);
}
