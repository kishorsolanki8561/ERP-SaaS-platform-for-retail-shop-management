using ErpSaas.Modules.Identity.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Identity.Controllers;

[Route("api/bootstrap")]
public sealed class BootstrapController(IBootstrapService bootstrapService) : BaseController
{
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var hasOwner = await bootstrapService.HasProductOwnerAsync(ct);
        return Ok(new { hasOwner });
    }

    [HttpPost("register-product-owner")]
    [AllowAnonymous]
    [RequireCaptcha]
    public async Task<IActionResult> RegisterProductOwner(
        [FromBody] RegisterOwnerRequest request, CancellationToken ct)
        => Ok(await bootstrapService.RegisterProductOwnerAsync(
            new RegisterOwnerDto(request.Name, request.Email, request.Password), ct));
}

public record RegisterOwnerRequest(string Name, string Email, string Password);
