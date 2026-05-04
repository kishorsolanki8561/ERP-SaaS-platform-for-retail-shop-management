using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.CustomerPortal.Services;

public interface ICustomerPortalService
{
    Task<Result<CustomerProfileDto>> GetProfileAsync(long platformCustomerId, CancellationToken ct = default);
    Task<Result<bool>> UpdateProfileAsync(long platformCustomerId, UpdateCustomerProfileDto dto, CancellationToken ct = default);
    Task<PagedResult<PurchaseHistoryDto>> ListPurchasesAsync(long platformCustomerId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<PurchaseDetailDto?>> GetPurchaseAsync(long platformCustomerId, long invoiceId, CancellationToken ct = default);
    Task<Result<CustomerInsightsDto>> GetInsightsAsync(long platformCustomerId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<PagedResult<LinkedShopDto>> ListLinkedShopsAsync(long platformCustomerId, int page, int pageSize, CancellationToken ct = default);
}

public record CustomerProfileDto(long Id, string? DisplayName, string? Email, string? Phone, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);
public record UpdateCustomerProfileDto(string? DisplayName, string? Email);
public record PurchaseHistoryDto(long InvoiceId, string InvoiceNumber, long ShopId, string ShopName, DateTime InvoiceDate, decimal GrandTotal, string Status);
public record PurchaseDetailDto(long InvoiceId, string InvoiceNumber, string ShopName, DateTime InvoiceDate, decimal SubTotal, decimal GrandTotal, IReadOnlyList<PurchaseLineDto> Lines);
public record PurchaseLineDto(string ProductName, string UnitCode, decimal Qty, decimal UnitPrice, decimal LineTotal);
public record CustomerInsightsDto(decimal TotalSpend, int TotalInvoices, IReadOnlyList<SpendByShopDto> ByShop);
public record SpendByShopDto(long ShopId, string ShopName, decimal Spend, int Invoices);
public record LinkedShopDto(long ShopId, string ShopName, bool IsPreferred, bool IsHiddenFromPortal, decimal WalletBalance, DateTime LinkedAtUtc);
