namespace ErpSaas.Shared.Data;

public record ShopInfoSnapshot(
    string LegalName,
    string? TradeName,
    string? GstNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateCode,
    string? PinCode,
    string? Phone,
    string? LogoUrl = null);

public interface IShopInfoProvider
{
    Task<ShopInfoSnapshot?> GetAsync(long shopId, CancellationToken ct = default);
}
