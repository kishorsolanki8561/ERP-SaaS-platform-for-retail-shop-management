using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.CustomerPortal.Services;

public interface ICustomerPortalAuthService
{
    Task<Result<OtpChallengeResult>> RequestOtpAsync(string identifier, CancellationToken ct = default);
    Task<Result<CustomerTokenResult>> VerifyOtpAsync(string identifier, string otp, string? deviceFingerprint, CancellationToken ct = default);
    Task<Result<CustomerTokenResult>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default);
}

public record OtpChallengeResult(string ChallengeId, DateTime ExpiresAtUtc);
public record CustomerTokenResult(string AccessToken, string RefreshToken, long PlatformCustomerId, string DisplayName);
