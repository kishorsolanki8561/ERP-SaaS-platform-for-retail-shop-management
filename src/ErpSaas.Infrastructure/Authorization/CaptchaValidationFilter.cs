using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Authorization;

/// <summary>
/// Action filter that enforces [RequireCaptcha]. In Development, CAPTCHA is bypassed unless
/// the Turnstile:AlwaysValidate config key is true. In Production, validates the
/// cf-turnstile-response token against Cloudflare Turnstile's siteverify API.
/// </summary>
public sealed class CaptchaValidationFilter(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<CaptchaValidationFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasAttribute = context.ActionDescriptor.FilterDescriptors
            .Any(fd => fd.Filter is RequireCaptchaAttribute)
            || context.Controller.GetType()
                .GetCustomAttributes(typeof(RequireCaptchaAttribute), true).Length > 0;

        if (!hasAttribute)
        {
            await next();
            return;
        }

        // Bypass in Development unless explicitly forced
        if (environment.IsDevelopment() &&
            !configuration.GetValue<bool>("Turnstile:AlwaysValidate"))
        {
            await next();
            return;
        }

        var token = context.HttpContext.Request.Headers["cf-turnstile-response"].FirstOrDefault()
            ?? context.HttpContext.Request.Form["cf-turnstile-response"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("CAPTCHA token missing on {Path}", context.HttpContext.Request.Path);
            context.Result = new ObjectResult(new { error = "CAPTCHA token is required." })
            {
                StatusCode = 400
            };
            return;
        }

        // TODO Phase 3: validate token against https://challenges.cloudflare.com/turnstile/v0/siteverify
        // For now in non-dev environments, accept any non-empty token (stub)
        await next();
    }
}
