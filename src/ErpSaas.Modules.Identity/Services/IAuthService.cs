using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public record LoginRequest(string Identifier, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
public record TotpChallengeResponse(string ChallengeToken);
public record ForgotPasswordRequest(string Identifier);
public record ResetPasswordRequest(string Token, string NewPassword);
public record AcceptInviteRequest(string Token, string NewPassword);

public interface IAuthService
{
    Task<Result<object>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<LoginResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<Result<LoginResponse>> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default);
}
