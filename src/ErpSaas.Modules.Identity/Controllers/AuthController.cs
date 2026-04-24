using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("login")]
    [AllowAnonymous]
    [RequireCaptcha]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await authService.LoginAsync(request, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        => Ok(await authService.RefreshAsync(request.RefreshToken, ct));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
        => Ok(await authService.LogoutAsync(request.RefreshToken, ct));
}

public record RefreshRequest(string RefreshToken);
