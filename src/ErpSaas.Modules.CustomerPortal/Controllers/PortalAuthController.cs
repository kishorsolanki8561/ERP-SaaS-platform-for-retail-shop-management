using ErpSaas.Modules.CustomerPortal.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.CustomerPortal.Controllers;

[Route("api/portal/auth")]
[ApiController]
public sealed class PortalAuthController(ICustomerPortalAuthService authService) : BaseController
{
    [HttpPost("signup-otp")]
    [AllowAnonymous]
    [RequireCaptcha]
    public async Task<IActionResult> RequestOtp([FromBody] PortalOtpRequest request, CancellationToken ct)
        => Ok(await authService.RequestOtpAsync(request.Identifier, ct));

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [RequireCaptcha]
    public async Task<IActionResult> VerifyOtp([FromBody] PortalVerifyOtpRequest request, CancellationToken ct)
        => Ok(await authService.VerifyOtpAsync(request.Identifier, request.Otp, request.DeviceFingerprint, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] PortalRefreshRequest request, CancellationToken ct)
        => Ok(await authService.RefreshAsync(request.RefreshToken, ct));

    [HttpPost("logout")]
    [CustomerAuth]
    public async Task<IActionResult> Logout([FromBody] PortalRefreshRequest request, CancellationToken ct)
        => Ok(await authService.LogoutAsync(request.RefreshToken, ct));
}

public record PortalOtpRequest(string Identifier);
public record PortalVerifyOtpRequest(string Identifier, string Otp, string? DeviceFingerprint);
public record PortalRefreshRequest(string RefreshToken);
