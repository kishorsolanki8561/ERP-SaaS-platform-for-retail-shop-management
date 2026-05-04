using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace ErpSaas.Infrastructure.Authorization;

/// <summary>
/// Overrides the default authorization result handler so that a feature-gate failure
/// (missing subscription plan feature) returns HTTP 402 Payment Required instead of 403
/// Forbidden. Permission failures remain 403.
///
/// Rules:
///   - Unauthenticated → 401 (falls through to default challenge)
///   - Authenticated, permission missing → 403 (falls through to default forbid)
///   - Authenticated, feature missing → 402 (handled here)
///   - Authenticated, both missing → 402 (feature is the subscription signal; permission
///     failure is not shown if the feature check also fails, but the 402 is more actionable)
/// </summary>
public sealed class SubscriptionAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // Only intercept authenticated users whose request was forbidden (not 401 challenge).
        if (!authorizeResult.Succeeded
            && context.User.Identity?.IsAuthenticated == true
            && authorizeResult.AuthorizationFailure?.FailedRequirements
                   .OfType<RequireFeatureAttribute>().Any() == true)
        {
            context.Response.StatusCode  = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"type":"subscription_required","title":"This feature is not included in your current subscription plan.","status":402}""");
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
