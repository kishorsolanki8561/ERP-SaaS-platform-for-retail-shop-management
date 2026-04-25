using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ErpSaas.Infrastructure.Authorization;

/// <summary>
/// Action filter that enforces [RequireCaptcha]. In Development, CAPTCHA is bypassed unless
/// the Turnstile:AlwaysValidate config key is true. In Production, validates the
/// cf-turnstile-response token against Cloudflare Turnstile's siteverify API.
/// </summary>
public sealed class CaptchaValidationFilter(
    IConfiguration configuration,
    IHostEnvironment environment,
    IHttpClientFactory httpClientFactory,
    ILogger<CaptchaValidationFilter> logger) : IAsyncActionFilter
{
    private const string SiteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

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

        var secretKey = configuration["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            // No secret configured — skip validation rather than blocking all requests.
            // Log a warning so the ops team knows to configure the key.
            logger.LogWarning("Turnstile:SecretKey is not configured; skipping CAPTCHA validation");
            await next();
            return;
        }

        var valid = await ValidateWithCloudflareAsync(token, secretKey, context.HttpContext.Connection.RemoteIpAddress?.ToString());
        if (!valid)
        {
            logger.LogWarning("CAPTCHA validation failed on {Path}", context.HttpContext.Request.Path);
            context.Result = new ObjectResult(new { error = "CAPTCHA validation failed." })
            {
                StatusCode = 400
            };
            return;
        }

        await next();
    }

    private async Task<bool> ValidateWithCloudflareAsync(string token, string secret, string? remoteIp)
    {
        try
        {
            var client = httpClientFactory.CreateClient("turnstile");
            var payload = new Dictionary<string, string>
            {
                ["secret"]   = secret,
                ["response"] = token,
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
                payload["remoteip"] = remoteIp;

            var response = await client.PostAsync(
                SiteverifyUrl,
                new FormUrlEncodedContent(payload));

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile siteverify call failed; failing open to avoid availability outage");
            // Fail open: if Cloudflare is unreachable we let the request through
            // rather than blocking all users. Alert + monitor via error logs.
            return true;
        }
    }

    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; init; }
    }
}
