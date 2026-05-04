using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public record OnboardShopRequest(
    string ShopCode,
    string LegalName,
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword,
    string? TradeName = null,
    string? GstNumber = null);

public interface IShopOnboardingService
{
    Task<Result<long>> OnboardAsync(OnboardShopRequest request, CancellationToken ct = default);

    Task<Result<long>> OnboardFromApprovedRequestAsync(
        ShopRegistrationRequest request, CancellationToken ct = default);
}
