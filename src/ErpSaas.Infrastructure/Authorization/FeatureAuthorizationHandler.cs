using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace ErpSaas.Infrastructure.Authorization;

public sealed class FeatureAuthorizationHandler
    : AuthorizationHandler<RequireFeatureAttribute>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireFeatureAttribute requirement)
    {
        var featsClaim = context.User.FindFirst("feats")?.Value ?? "";
        var userFeats = featsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (userFeats.Contains(requirement.FeatureCode, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
