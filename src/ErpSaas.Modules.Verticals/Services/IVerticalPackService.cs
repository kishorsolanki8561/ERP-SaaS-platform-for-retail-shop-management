using ErpSaas.Infrastructure.Data.Entities.Verticals;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Verticals.Services;

public record VerticalPackDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    string FeatureFlagsCsv,
    string? IconClass,
    int SortOrder,
    bool IsActive);

public record ShopVerticalDto(
    long Id,
    long VerticalPackId,
    string VerticalPackCode,
    string VerticalPackName,
    DateTime AppliedAtUtc);

public interface IVerticalPackService
{
    Task<IReadOnlyList<VerticalPackDto>> ListPacksAsync(CancellationToken ct = default);
    Task<VerticalPackDto?> GetPackAsync(string code, CancellationToken ct = default);
    Task<ShopVerticalDto?> GetShopVerticalAsync(CancellationToken ct = default);
    Task<Result<long>> InstallForShopAsync(string packCode, CancellationToken ct = default);
}
