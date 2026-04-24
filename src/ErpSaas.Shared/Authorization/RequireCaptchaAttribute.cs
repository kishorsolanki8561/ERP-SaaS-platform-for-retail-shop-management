using Microsoft.AspNetCore.Mvc.Filters;

namespace ErpSaas.Shared.Authorization;

/// <summary>
/// Marks an endpoint as requiring Cloudflare Turnstile CAPTCHA validation.
/// The CaptchaValidationFilter reads the cf-turnstile-response header/body field
/// and validates it against the Turnstile secret API.
/// Apply to every public auth surface (login, forgot-password, signup, OTP, bootstrap).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireCaptchaAttribute : Attribute, IFilterMetadata { }
