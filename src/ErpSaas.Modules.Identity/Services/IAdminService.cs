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

public record BranchDto(
    long Id,
    string Name,
    string? City,
    string? Phone,
    bool IsActive,
    bool IsHeadOffice);

public record CreateBranchDto(
    string Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? StateCode = null,
    string? PinCode = null,
    string? Phone = null,
    string? GstNumber = null,
    bool IsHeadOffice = false);

public record UpdateBranchDto(
    string Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? StateCode = null,
    string? PinCode = null,
    string? Phone = null,
    string? GstNumber = null);

public record InviteUserDto(
    string DisplayName,
    string Email,
    string? Phone = null,
    long? RoleId = null);

public interface IAdminService
{
    Task<ShopProfileDto?> GetShopProfileAsync(CancellationToken ct = default);
    Task<Result<bool>> UpdateShopProfileAsync(UpdateShopProfileDto dto, CancellationToken ct = default);
    Task<PagedResult<AdminUserDto>> ListUsersAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<bool>> DeactivateUserAsync(long userId, CancellationToken ct = default);
    Task<Result<long>> InviteUserAsync(InviteUserDto dto, CancellationToken ct = default);
    Task<Result<bool>> ResendInviteAsync(long userId, CancellationToken ct = default);
    Task<Result<bool>> ForceResetPasswordAsync(long userId, CancellationToken ct = default);
    Task<Result<bool>> UnlockUserAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateRolePermissionsAsync(long roleId, UpdateRolePermissionsDto dto, CancellationToken ct = default);
    Task<Result<bool>> AssignUserRoleAsync(long userId, long roleId, CancellationToken ct = default);
    Task<Result<bool>> RemoveUserRoleAsync(long userId, long roleId, CancellationToken ct = default);

    Task<IReadOnlyList<BranchDto>> ListBranchesAsync(CancellationToken ct = default);
    Task<Result<long>> CreateBranchAsync(CreateBranchDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateBranchAsync(long branchId, UpdateBranchDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeactivateBranchAsync(long branchId, CancellationToken ct = default);
}
