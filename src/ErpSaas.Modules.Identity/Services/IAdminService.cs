using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public record ShopProfileDto(
    string ShopCode,
    string LegalName,
    string? TradeName,
    string? GstNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateCode,
    string? PinCode,
    string CurrencyCode,
    string TimeZone);

public record UpdateShopProfileDto(
    string LegalName,
    string? TradeName,
    string? GstNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateCode,
    string? PinCode,
    string CurrencyCode);

public record AdminUserDto(
    long Id,
    string DisplayName,
    string? Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<string> Roles);

public record PermissionDto(
    long Id,
    string Code,
    string Module,
    string Label);

public record RoleDto(
    long Id,
    string Code,
    string Label,
    bool IsSystemRole,
    IReadOnlyList<string> PermissionCodes);

public record CreateRoleDto(
    string Code,
    string Label);

public record UpdateRolePermissionsDto(
    IReadOnlyList<string> PermissionCodes);

public interface IAdminService
{
    Task<ShopProfileDto?> GetShopProfileAsync(CancellationToken ct = default);
    Task<Result<bool>> UpdateShopProfileAsync(UpdateShopProfileDto dto, CancellationToken ct = default);
    Task<PagedResult<AdminUserDto>> ListUsersAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<bool>> DeactivateUserAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateRolePermissionsAsync(long roleId, UpdateRolePermissionsDto dto, CancellationToken ct = default);
    Task<Result<bool>> AssignUserRoleAsync(long userId, long roleId, CancellationToken ct = default);
    Task<Result<bool>> RemoveUserRoleAsync(long userId, long roleId, CancellationToken ct = default);
}
