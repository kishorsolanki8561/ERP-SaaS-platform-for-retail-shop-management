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
    bool IsActive);

public interface IAdminService
{
    Task<ShopProfileDto?> GetShopProfileAsync(CancellationToken ct = default);
    Task<Result<bool>> UpdateShopProfileAsync(UpdateShopProfileDto dto, CancellationToken ct = default);
    Task<PagedResult<AdminUserDto>> ListUsersAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
}
