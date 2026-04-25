namespace ErpSaas.Modules.Identity.Services;

public record MenuItemDto(
    string Code,
    string Label,
    string Kind,
    string? Icon,
    string? Route,
    int SortOrder,
    IReadOnlyList<MenuItemDto> Children);

public interface IMenuService
{
    Task<IReadOnlyList<MenuItemDto>> GetTreeAsync(long userId, long shopId, bool isPlatformAdmin = false, CancellationToken ct = default);
}
